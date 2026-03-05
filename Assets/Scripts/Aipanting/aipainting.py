from __future__ import division
import torch
import torchvision
import torchvision.transforms as transforms
import torch.optim as optim
import torch.nn as nn
import torch.nn.functional as F
import random
import os
import kornia
import sys
import matplotlib.pyplot as plt
import numpy as np
import moviepy.editor as mpy
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")



from typing import List,Union


from PIL import Image

def paint_over_canvas(canvas: torch.Tensor, stroke: torch.Tensor):
  darkness_mask = torch.mean(stroke, dim=1, keepdim=True)  
  darkness_mask = 1. - darkness_mask
  normalizer, _ = torch.max(darkness_mask, dim=2, keepdim=True)
  normalizer, _ = torch.max(normalizer, dim=3, keepdim=True)  
  darkness_mask = darkness_mask / normalizer
  blended = darkness_mask * stroke + (1-darkness_mask) * canvas
  return blended

def paint_over_canvas2(canvas: torch.Tensor, stroke: torch.Tensor, color: torch.Tensor):
  _, _, canvas_height, canvas_width = canvas.shape
  darkness_mask = torch.mean(stroke, dim=1, keepdim=True)  
  darkness_mask = 1. - darkness_mask
  normalizer, _ = torch.max(darkness_mask, dim=2, keepdim=True)
  normalizer, _ = torch.max(normalizer, dim=3, keepdim=True) 
  darkness_mask = darkness_mask / normalizer

  color_action = color.view(-1, 3, 1, 1)
  color_action = color_action.repeat(1, 1, canvas_height, canvas_width)

  blended = darkness_mask * color_action + (1-darkness_mask) * canvas
  return blended


class NeuralCanvas(nn.Module):
  """
  NeuralCanvas is a simple wrapper around a NeuralPainter. Maps a sequence of brushstrokes to a full canvas.
  Automatically performs blending.
  """
  def __init__(self, neural_painter, action_preprocessor=torch.sigmoid):
    """
    neural_painter: Neural painter to wrap
    action_preprocessor: Set the action preprocessor for this canvas. It is called on the input tensor before passing on
    to the actual neural painter. This is where one can specify manual constraints on actions e.g. grayscale strokes,
    controlling stroke thickness, etc. By default this canvas uses torch.sigmoid to make sure input actions are in the
    range [0, 1] i.e. backprop can make the input actions go beyond this range. We suggest you call sigmoid() somewhere
    as well if you plan to use your own action preprocessor.
    """
    super(NeuralCanvas, self).__init__()

    self.neural_painter = neural_painter
    self.canvas_height = 64
    self.canvas_width = 64
    self.final_canvas_height = 64
    self.final_canvas_width = 64

    self.action_preprocessor = action_preprocessor

  def forward(self, actions: torch.Tensor):
    """
    actions: tensor of shape (num_strokes, batch_size, action_size)

    Returns tuple of:
      final_canvas: Image tensor of shape (batch_size, height, width). contains final canvas.
      intermediate_canvases: List of length num_strokes of image tensors of shape (batch_size, height, width).
                             Each tensor in the list represents one stroke. Used for visualization.
    """
    actions = self.action_preprocessor(actions)
    num_strokes, batch_size, action_size = actions.shape

    intermediate_canvases = []
    next_canvas = torch.ones(batch_size, 3, self.final_canvas_height, self.final_canvas_width).to(actions.device)
    intermediate_canvases.append(next_canvas.detach().cpu())
    for i in range(num_strokes):
      stroke = self.neural_painter(actions[i].view(-1, 12, 1, 1)).view(-1, 3, 64, 64)
      next_canvas = paint_over_canvas(next_canvas, stroke, actions[i, :, 6:9])
      intermediate_canvases.append(next_canvas.detach().cpu())

    return next_canvas, intermediate_canvases
  
class NeuralCanvasStitched(nn.Module):
  """
  NeuralCanvasStitched is a collection of NeuralCanvas stitched together. Used to get higher resolution images from a
  low-res 64px neural painter.
  Maps a sequence of brushstrokes to a fully stitched canvas.
  """
  def __init__(self, neural_painter, overlap_px=10, repeat_h=8, repeat_w=8, strokes_per_block=5, action_preprocessor=torch.sigmoid):
    """
    neural_painter: neural painter to wrap
    overlap_px: number of overlapping pixels between canvases
    repeat_h: number of canvases stitched together for height of final canvas
    repeat_w: number of canvases stitched together for width of final canvas
    strokes_per_block: Number of strokes per canvas.
    action_preprocessor: Set the action preprocessor for this canvas. It is called on the input tensor before passing on
    to the actual neural painter. This is where one can specify manual constraints on actions e.g. grayscale strokes,
    controlling stroke thickness, etc. By default this canvas uses torch.sigmoid to make sure input actions are in the
    range [0, 1] i.e. backprop can make the input actions go beyond this range. We suggest you call sigmoid() somewhere
    as well if you plan to use your own action preprocessor.
    """
    super(NeuralCanvasStitched, self).__init__()

    self.neural_painter = neural_painter
    self.overlap_px = overlap_px
    self.repeat_h = repeat_h
    self.repeat_w = repeat_w
    self.strokes_per_block = strokes_per_block

    self.final_canvas_h = 64*repeat_h - overlap_px*(repeat_h - 1)
    self.final_canvas_w = 64*repeat_w - overlap_px*(repeat_w - 1)
    self.total_num_strokes = strokes_per_block * repeat_h * repeat_w


    print(f'final canvas size H: {self.final_canvas_h} W: {self.final_canvas_w}\t'
          f'total number of strokes: {self.total_num_strokes}')

  def forward(self, actions: torch.Tensor, next_canvas):
    """
    actions: tensor of shape (num_strokes, batch_size, action_size)

    Returns tuple of:
      final_canvas: Image tensor of shape (batch_size, height, width). contains final canvas.
      intermediate_canvases: List of length total_num_strokes of image tensors of shape (batch_size, height, width).
                             Each tensor in the list represents one stroke. Used for visualization.
    """
    num_strokes, batch_size, action_size = actions.shape
    assert num_strokes == self.total_num_strokes

    intermediate_canvases = []
    intermediate_canvases.append(next_canvas.detach().cpu())

    block_ctr = 0
    for a in range(self.repeat_h):
      for b in range(self.repeat_w):
        for local_stroke_idx in range(self.strokes_per_block):
          c = block_ctr * self.strokes_per_block + local_stroke_idx
          current_action = actions[c]
          decoded_stroke = (self.neural_painter(current_action)[1]).view(-1, 3, 64, 64)
          padding = nn.ConstantPad2d(
            [(64-self.overlap_px)*b,
             (64-self.overlap_px)*(self.repeat_w-1-b),
             (64-self.overlap_px)*a,
             (64-self.overlap_px)*(self.repeat_h-1-a)], 1)
          padded_stroke = padding(decoded_stroke)
          next_canvas = paint_over_canvas(next_canvas, padded_stroke)
        intermediate_canvases.append(next_canvas.detach().cpu())  
        block_ctr += 1
    return next_canvas, intermediate_canvases

class RandomScale(nn.Module):
  """Module for randomly scaling an image"""
  def __init__(self, scales):
    """
    :param scales: list of scales to randomly choose from e.g. [0.8, 1.0, 1.2] will randomly scale an image by
      0.8, 1.0, or 1.2
    """
    super(RandomScale, self).__init__()

    self.scales = scales

  def forward(self, x: torch.Tensor):
    scale = self.scales[random.randint(0, len(self.scales)-1)]
    return F.interpolate(x, scale_factor=scale, mode='bilinear')


class RandomCrop(nn.Module):
  """Module for randomly cropping an image"""
  def __init__(self, size: int):
    """
    :param size: How much to crop from both sides. e.g. 8 will remove 8 pixels in both x and y directions.
    """
    super(RandomCrop, self).__init__()
    self.size = size

  def forward(self, x: torch.Tensor):
    batch_size, _, h, w = x.shape
    h_move = random.randint(0, self.size)
    w_move = random.randint(0, self.size)
    return x[:, :, h_move:h-self.size+h_move, w_move:w-self.size+w_move]


class RandomRotate(nn.Module):
  """Module for randomly rotating an image"""
  def __init__(self, angle=10, same_throughout_batch=False):
    """
    :param angle: Angle in degrees
    :param same_throughout_batch: Degree of rotation, although random, is kept the same throughout a single batch.
    """
    super(RandomRotate, self).__init__()
    self.angle=angle
    self.same_throughout_batch = same_throughout_batch

  def forward(self, img: torch.tensor):
    b, _, h, w = img.shape
    if not self.same_throughout_batch:
      angle = torch.randn(b, device=img.device) * self.angle
    else:
      angle = torch.randn(1, device=img.device) * self.angle
      angle = angle.repeat(b)
    center = torch.ones(b, 2, device=img.device)
    center[..., 0] = img.shape[3] / 2  
    center[..., 1] = img.shape[2] / 2  
    scale = torch.ones(b, device=img.device)
    M = kornia.get_rotation_matrix2d(center, angle, scale)
    img_warped = kornia.warp_affine(img, M, dsize=(h, w))
    return img_warped


class Normalization(nn.Module):
  """Normalization module"""
  def __init__(self, mean, std):
    super(Normalization, self).__init__()
    self.mean = torch.tensor(mean).view(-1, 1, 1)
    self.std = torch.tensor(std).view(-1, 1, 1)

  def forward(self, img):
    return (img - self.mean) / self.std


def plot_images(images, figsize=(16, 16)):
  fig=plt.figure(figsize=figsize)
  columns = len(images)

  for i, img in enumerate(images):
    img = img[:, :, :3]
    fig.add_subplot(1, columns, i+1)
    plt.grid(False)
    plt.imshow(img)
  plt.show()


def animate_frames(frames, video_path):
  def frame(t):
    t = int(t * 10.)
    if t >= len(frames):
      t = len(frames) - 1
    return frames[t]

  clip = mpy.VideoClip(frame, duration=len(frames) // 10.)
  clip.write_videofile(video_path, fps=10.)


def validate_neural_painter(strokes, actions, neural_painter, checkpoints_to_test):
  for ckpt in checkpoints_to_test:
    neural_painter.load_from_train_checkpoint(ckpt)
    with torch.no_grad():
      pred_strokes = neural_painter(actions)

    plot_images(np.transpose(strokes.numpy(), [0, 2, 3, 1]))
    plot_images(np.transpose(pred_strokes.numpy(), [0, 2, 3, 1]))


def neural_painter_stroke_animation(neural_painter_fn,
                                    action_size,
                                    checkpoints_to_test,
                                    video_path,
                                    num_acs=8,
                                    duration=1.0,
                                    fps=30.0,
                                    real_env=None):
  if real_env:
    real_env.reset()
  acs = np.random.uniform(size=[num_acs, action_size])

  neural_painters = []
  for ckpt in checkpoints_to_test:
    x = neural_painter_fn()
    x.load_from_train_checkpoint(ckpt)
    neural_painters.append(x)

  def frame(t):
    t_ = t / duration
    t = np.abs((1.0 - np.cos(num_acs * np.pi * np.mod(t_, 1. / num_acs))) / 2.0)

    new_ac = (1 - t) * acs[int(np.floor(t_ * num_acs))] + t * acs[int((np.floor(t_ * num_acs) + 1) % num_acs)]
    if real_env:
      real_env.draw(new_ac)
      im = real_env.image
      im = im[:, :, :3]
    stack_these = []
    for neural_painter in neural_painters:
      with torch.no_grad():
        decoded = neural_painter(torch.FloatTensor([new_ac]))
      decoded = np.transpose(decoded.numpy(), [0, 2, 3, 1])[0]
      decoded = (decoded * 255).astype(np.uint8)
      if real_env:
        decoded = np.concatenate([im, decoded], 1)
      stack_these.append(decoded)
    return np.concatenate(stack_these, axis=0)

  clip = mpy.VideoClip(frame, duration=duration)
  clip.write_videofile(video_path, fps=fps)
  print("written to {}".format(video_path))


def animate_strokes_on_canvas(intermediate_canvases: List[torch.Tensor],
                              target_image: Union[torch.tensor, None],
                              video_path: str, skip_every_n: int = 1,
                              batch_idx: int = 0):
  _, _, h, w = intermediate_canvases[0].shape

  intermediate_canvases = [(x.detach().cpu().numpy() * 255).astype(np.uint8).transpose(0, 2, 3, 1)[batch_idx]
                           for x in intermediate_canvases]
  intermediate_canvases = np.stack(intermediate_canvases)
  intermediate_canvases = intermediate_canvases[::skip_every_n]

  to_plot = intermediate_canvases

  if target_image is not None:
    target_images = (target_image.detach().cpu().numpy() * 255).astype(np.uint8).reshape(1, 3, h, w).transpose(0, 2, 3, 1)
    target_images = np.tile(target_images, [len(intermediate_canvases), 1, 1, 1])
    to_plot = np.concatenate([target_images, to_plot], axis=(2 if h >= w else 1))

  to_plot = np.concatenate([to_plot, np.tile(to_plot[-1:, :, :, :], [50, 1, 1, 1])], axis=0)

  animate_frames(to_plot, video_path)



def weights_init(m):
    classname = m.__class__.__name__
    if classname.find('Conv') != -1:
        nn.init.normal_(m.weight.data, 0.0, 0.02)
    elif classname.find('BatchNorm') != -1:
        nn.init.normal_(m.weight.data, 1.0, 0.02)
        nn.init.constant_(m.bias.data, 0)

class Generator(nn.Module):
    def __init__(self, nz, ngf, nc):
        super(Generator, self).__init__()
        self.nz = nz-3
        self.ngf = ngf
        self.nc = nc
        self.fc1 = nn.Linear(self.nz, 4 * 4 * (ngf * 8))  
        self.bn1 = nn.BatchNorm2d(ngf * 8)
        self.relu1 = nn.ReLU(True)
        self.main = nn.Sequential(
            nn.ConvTranspose2d(ngf * 8, ngf * 4, 4, 2, 1, bias=False),
            nn.BatchNorm2d(ngf * 4),
            nn.ReLU(True),
            nn.ConvTranspose2d( ngf * 4, ngf * 2, 4, 2, 1, bias=False),
            nn.BatchNorm2d(ngf * 2),
            nn.ReLU(True),
            nn.ConvTranspose2d( ngf * 2, ngf, 4, 2, 1, bias=False),
            nn.BatchNorm2d(ngf),
            nn.ReLU(True),
            nn.ConvTranspose2d( ngf, nc, 4, 2, 1, bias=False),
            nn.Tanh()
        )

    def forward(self, input):
        x = self.fc1(input[:,3:])
        x = x.view(-1, self.ngf * 8, 4, 4)
        x = self.relu1(self.bn1(x))
        x = self.main(x)
        darkness_mask = torch.mean(x, dim=1, keepdim=True)
        darkness_mask = 1. - darkness_mask
        color = torch.sigmoid(input[:, :3].view(-1, 3, 1, 1))

        z = darkness_mask * color + (1. - darkness_mask)
        return x, z

class Discriminator(nn.Module):
    def __init__(self, ndf, nc):
        super(Discriminator, self).__init__()
        self.main = nn.Sequential(
            nn.Conv2d(nc, ndf, 4, 2, 1, bias=False),
            nn.LeakyReLU(0.2, inplace=True),
            nn.Conv2d(ndf, ndf * 2, 4, 2, 1, bias=False),
            nn.BatchNorm2d(ndf * 2),
            nn.LeakyReLU(0.2, inplace=True),
            nn.Conv2d(ndf * 2, ndf * 4, 4, 2, 1, bias=False),
            nn.BatchNorm2d(ndf * 4),
            nn.LeakyReLU(0.2, inplace=True),
            nn.Conv2d(ndf * 4, ndf * 8, 4, 2, 1, bias=False),
            nn.BatchNorm2d(ndf * 8),
            nn.LeakyReLU(0.2, inplace=True),
            nn.Conv2d(ndf * 8, 1, 4, 1, 0, bias=False),
        )

    def forward(self, input):
        x = self.main(input)
        x = torch.sigmoid(x)
        return x


device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(torch.cuda.is_available())
ACTIONS_TO_IDX = {
    'pressure': 0,
    'size': 1,
    'control_x': 2,
    'control_y': 3,
    'end_x': 4,
    'end_y': 5,
    'color_r': 6,
    'color_g': 7,
    'color_b': 8,
    'start_x': 9,
    'start_y': 10,
    'entry_pressure': 11,
    'pressure2': 12,
    'size2': 13,
    'control_x2': 14,
    'control_y2': 15,
    'end_x2': 16,
    'end_y2': 17,
    'color_r2': 18,
    'color_g2': 19,
    'color_b2': 20,
    'start_x2': 21,
    'start_y2': 22,
    'entry_pressure2': 23,
    'entry_pressure3': 24,
    'pressure3': 25,
    'size3': 26,
    'control_x3': 27,
    'control_y3': 28,
    'end_x3': 29,
    'end_y3': 30,
    'color_r3': 31,
    'color_g3': 32,
    'color_b3': 33,
    'start_x3': 34,
    'start_y3': 35,
    'entry_pressure33': 36,
    'pressure23': 37,
    'size23': 38,
    'control_x23': 39,
    'control_y23': 40,
    'end_x23': 41,
    'end_y23': 42,
    'color_r23': 43,
    'color_g23': 44,
    'color_b23': 45,
    'start_x23': 46,
    'start_y23': 47,
    'entry_pressure23': 48,
    'entry_pressure333': 49,
}

inception_v1 = torch.hub.load('pytorch/vision:v0.9.0', 'googlenet', pretrained=True)
resnet18 = torch.hub.load('pytorch/vision:v0.9.0', 'resnet18', pretrained=True)
vgg19 = torch.hub.load('pytorch/vision:v0.9.0', 'vgg19', pretrained=True)

STROKES_PER_BLOCK =3  
REPEAT_CANVAS_HEIGHT = 3 
REPEAT_CANVAS_WIDTH = 3

LAYER = "3B" 
LAYER_IDX = -12 if LAYER == "3B" else -13

STOCHASTIC = False 

NORMALIZE = True 
LEARNING_RATE = 0.1 

current_path = os.path.dirname(os.path.abspath(__file__))
img_path =  current_path + '/images' 
output_path = 'Assets/Scripts/Aipanting/results1'

for img_name in os.listdir(img_path):
    file_name = img_name 
    file_name = file_name.split('.')[0] 
    output_path_new = output_path+"/"+file_name
    if not os.path.exists(output_path_new):
        os.makedirs(output_path_new)

    img = img_path+"/"+file_name+'.jpg'



    print('STROKES_PER_BLOCK: {}'.format(STROKES_PER_BLOCK))
    print("REPEAT_CANVAS_HEIGHT", REPEAT_CANVAS_HEIGHT)
    print("REPEAT_CANVAS_WIDTH", REPEAT_CANVAS_WIDTH)
    print('LAYER: {}'.format(LAYER))
    print('STOCHASTIC: {}'.format(STOCHASTIC))
    print('NORMALIZE: {}'.format(NORMALIZE))
    print('LEARNING RATE: {}'.format(LEARNING_RATE))
    print('IMAGE NAME: {}'.format(img))


    neural_painter = Generator(len(ACTIONS_TO_IDX), 64, 3).to(device)
    neural_painter.load_state_dict(torch.load('Assets/Scripts/Aipanting/sgan/sdcgan50_4b2_fc_10.tar'))

    normalizer = Normalization(torch.tensor([0.5, 0.5, 0.5]).to(device),
                               torch.tensor([0.5, 0.5, 0.5]).to(device))

    padder = nn.ConstantPad2d(12, 0.5)
    rand_crop_8 = RandomCrop(8)
    rand_scale = RandomScale([1 + (i - 5) / 50. for i in range(11)])
    random_rotater = RandomRotate(angle=5, same_throughout_batch=True)
    rand_crop_4 = RandomCrop(4)


    feature_extractor = nn.Sequential(*list(inception_v1.children())[:LAYER_IDX])
    feature_extractor.eval().to(device)
    feature_extractor_res = nn.Sequential(*list(resnet18.children())[:4])
    feature_extractor_res.eval().to(device)



    action_preprocessor = torch.sigmoid  
    canvas = NeuralCanvasStitched(neural_painter=neural_painter, overlap_px=32,
                                  repeat_h=REPEAT_CANVAS_HEIGHT, repeat_w=REPEAT_CANVAS_WIDTH,
                                  strokes_per_block=STROKES_PER_BLOCK,
                                  action_preprocessor=action_preprocessor)

    image = Image.open(img)
    loader = transforms.Compose([
        transforms.Resize([canvas.final_canvas_h, canvas.final_canvas_w]),  
        transforms.ToTensor()])  
    image = loader(image).unsqueeze(0)[:, :3, :, :].to(device, torch.float)

    actions = torch.FloatTensor(canvas.total_num_strokes, 1, len(ACTIONS_TO_IDX)).uniform_().to(device)

    optimizer = optim.Adam([actions.requires_grad_()], lr=LEARNING_RATE)
    output_canvas = torch.ones(1, 3, canvas.final_canvas_h, canvas.final_canvas_w).to(actions.device)
    loss_fn = torch.nn.L1Loss()

    f = open('Assets/Scripts/Aipanting/loss/gloss.txt', 'w')

    for idx in range(401):
        optimizer.zero_grad()
        output_canvas = torch.ones(1, 3, canvas.final_canvas_h, canvas.final_canvas_w).to(actions.device)
        output_canvas, _ = canvas(actions, output_canvas.detach())


        stacked_canvas = torch.cat([output_canvas, image])

        augmented_canvas = stacked_canvas

        output_features = feature_extractor(augmented_canvas)
        output_features_res = feature_extractor_res(augmented_canvas)

        cost = loss_fn(output_features[0], output_features[1]) * 0.1 + loss_fn(output_features_res[0], output_features_res[1]) * 0.9

        f.write(str(idx)+"\t"+str(cost.item())+"\t")
        cost.backward()
        optimizer.step()
        if idx % 5 == 0:
            print(f'Step {idx}\tCost {cost.item()}')
    f.close()
    output_canvas = torch.ones(1, 3, canvas.final_canvas_h, canvas.final_canvas_w).to(actions.device)
    _, intermediate_canvases = canvas(actions, output_canvas.detach())
    m = len(intermediate_canvases)
    for i in range(m):
        torchvision.utils.save_image(intermediate_canvases[i], output_path_new + "/"+ file_name + '_' + str(i) + '.png')
    animate_strokes_on_canvas(intermediate_canvases, None, output_path_new + "/" + file_name + '.mp4', skip_every_n=1) 
