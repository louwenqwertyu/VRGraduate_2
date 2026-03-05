import torch
import torchvision
import torchvision.transforms as transforms
import torch.optim as optim
import cv2
import os
from PIL import Image
import shutil
 
from torch.cuda.amp import autocast


from canvas import NeuralCanvas, NeuralCanvasStitched
from transforms import RandomRotate, Normalization, RandomCrop, RandomScale
from viz import *
from SDCGAN_sgmd import *


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

def pad(img, H, W):
    b, c, h, w = img.shape
    pad_h = (H - h) // 2
    pad_w = (W - w) // 2
    remainder_h = (H - h) % 2
    remainder_w = (W - w) % 2
    img = torch.cat([torch.zeros((b, c, pad_h, w), device=img.device), img,
                     torch.zeros((b, c, pad_h + remainder_h, w), device=img.device)], dim=-2)
    img = torch.cat([torch.zeros((b, c, H, pad_w), device=img.device), img,
                     torch.zeros((b, c, H, pad_w + remainder_w), device=img.device)], dim=-1)
    return img

inception_v1 = torch.hub.load('pytorch/vision:v0.9.0', 'googlenet', pretrained=True)
resnet18 = torch.hub.load('pytorch/vision:v0.9.0', 'resnet18', pretrained=True)

STROKES_PER_BLOCK = 12 

LAYER = "3B" 
LAYER_IDX = -12 if LAYER == "3A" else -13
STOCHASTIC = False 
NORMALIZE = True 
LEARNING_RATE = 0.099 
img_path = 'chi_img' 
mask_path = 'chi_mask'
edge_path = 'chi_egde'
output_path = 'ai_p_results'

for img_name in os.listdir(img_path):
    file_name = img_name 
    file_name = file_name.split('.')[0] 
    output_path_new = output_path+"/"+file_name
    if not os.path.exists(output_path_new):
        os.makedirs(output_path_new)

    img = img_path+"/"+file_name+'.png'
    mask = mask_path+"/"+file_name+'.png'
    edge = edge_path+"/"+file_name+'.png'


    print('STROKES_PER_BLOCK: {}'.format(STROKES_PER_BLOCK))
    print('LAYER: {}'.format(LAYER))
    print('LAYER_IDX: {}'.format(LAYER_IDX))
    print('STOCHASTIC: {}'.format(STOCHASTIC))
    print('NORMALIZE: {}'.format(NORMALIZE))
    print('LEARNING RATE: {}'.format(LEARNING_RATE))
    print('IMAGE NAME: {}'.format(img))


    neural_painter = Generator(len(ACTIONS_TO_IDX), 64, 3).to(device)
    neural_painter.load_state_dict(torch.load('sgan/mstroke.tar')) 


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

    os.environ['CUDA_LAUNCH_BLOCKING'] = '1'

    action_preprocessor = torch.sigmoid  
    canvas = []
    for i in range(0, 4):
        canvas.append(NeuralCanvasStitched(neural_painter=neural_painter, overlap_px=32,
                                repeat_h=2**(i+1)-1, repeat_w=2**(i+1)-1,
                                strokes_per_block=STROKES_PER_BLOCK,
                                action_preprocessor=action_preprocessor))

    o_image = Image.open(img)

    loader = transforms.Compose([
        transforms.ToTensor()])  
    image_o = loader(o_image).unsqueeze(0)[:, :3, :, :].to(device, torch.float)
    image_o = pad(image_o, 512, 512)
    mask = Image.open(mask)
    loader = transforms.Compose([
        transforms.ToTensor()]) 
    mask = loader(mask).unsqueeze(0)[:, :3, :, :].to(device, torch.float)
    mask = pad(mask, 512, 512)
    mask_bg = 1 - mask



    edge = Image.open(edge)
    print(edge)
    loader = transforms.Compose([
        transforms.ToTensor()])  
    edge = loader(edge).unsqueeze(0)[:, :3, :, :].to(device, torch.float)
    print(edge.shape)
    edge = pad(edge, 512, 512)

    output_canvas_o = torch.ones(1, 3, 512, 512).to(device)
    output_canvas = output_canvas_o
    print("output_canvas_shape:",output_canvas.shape)
    print("output_canvas_type:",output_canvas.type)
    image = image_o*mask
    image_bg = image_o*mask_bg

    loss_fn = torch.nn.L1Loss()

    n_pt = 800  
    intermediate_canvases = []
    intermediate_paint_fg = []
    intermediate_paint_bg = []
    for k in range(1, 4):
        output_canvas = F.interpolate(output_canvas, (64 * (2 ** k), 64 * (2 ** k)))
        temp_canvas = output_canvas
        image = F.interpolate(image_o, (64 * (2 ** k), 64 * (2 ** k)))
        actions = torch.FloatTensor(canvas[k].total_num_strokes, 1, len(ACTIONS_TO_IDX)).uniform_().to(device)
        optimizer = optim.Adam([actions.requires_grad_()], lr=LEARNING_RATE)
        for idx in range(n_pt+1):
            optimizer.zero_grad()
            if idx == n_pt:
                output_canvas, intermediate_canvase = canvas[k](actions, temp_canvas.detach(), True)
            else:
                output_canvas= canvas[k](actions, temp_canvas.detach())[0]
            stacked_canvas = torch.cat([output_canvas, image])
            augmented_canvas = stacked_canvas
            output_features = feature_extractor(augmented_canvas)
            output_features_res = feature_extractor_res(augmented_canvas)

            cost = loss_fn(output_features[0], output_features[1]) * 0.05 + loss_fn(output_features_res[0], output_features_res[1]) * 0.95 
            cost.backward()
            optimizer.step()
            if idx % 1 == 0:
                print(f'k {k}\tStep {idx}\tCost {cost.item()}')
        intermediate_canvases.extend(intermediate_canvase)

    n = len(intermediate_canvases)
    intermediate_paint_fg = [[] for _ in range(n)]
    intermediate_paint_bg = [[] for _ in range(n)]

    edge = edge*mask + mask_bg
    mask = mask.cpu()
    mask_bg = mask_bg.cpu()
    edge = edge.cpu()
    for idx in range(n):
        intermediate_paint_fg[idx] = intermediate_canvases[idx]*mask
        intermediate_paint_fg[idx] = intermediate_paint_fg[idx] + mask_bg
        intermediate_paint_bg[idx] = intermediate_canvases[idx]*mask_bg
        intermediate_paint_bg[idx] = intermediate_paint_bg[idx] + intermediate_canvases[n-1]*mask

    intermediate_paint_fg.extend(intermediate_paint_bg)
    m = len(intermediate_paint_fg)
    paint_step = [[]for _ in range(m)]
    for i in range(m):
       paint_step[i] = intermediate_paint_fg[i] 

    def con (dir_image1, dir_image2):
        image1 = np.array(dir_image1)
        image2 = np.array(dir_image2)
        if(np.array_equal(image1, image2)):
            result = "con_same"
        else:
            result = "con_diff"
        return result

    def remove_list(lista, listb):
        for x in listb:
            lista.remove(x)
        return lista

    def con_com (dir_image1, dir_image2):
        
        result = "con_diff"
        re = con(dir_image1, dir_image2)
        if(re == "con_same"):
            result = "con_same"
        return result
    file_repeat = []
    for currIndex, filename in enumerate(paint_step):
        dir_image1 = paint_step[currIndex]
        dir_image2 = paint_step[currIndex + 1]
        result = con_com(dir_image1, dir_image2)
        if(result == "con_same"):
            file_repeat.append(currIndex + 1)
        currIndex += 1
        if currIndex >= len(paint_step)-1:
            break

    print(len(paint_step))
    print(len(file_repeat))

    file_repeat = file_repeat[: :-1]
    for img in file_repeat:
        del paint_step[img]
    print(len(paint_step))
    for i in range(len(paint_step)):
        torchvision.utils.save_image(paint_step[i], output_path_new + "/"+ file_name + '_' + str(i) + '.png')
    
    animate_strokes_on_canvas(paint_step, None, output_path_new + "/" + file_name + '.mp4', skip_every_n=1) 
