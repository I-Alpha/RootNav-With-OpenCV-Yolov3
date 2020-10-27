# RootNav_OpencvYolov3

## Original Vanilla RootNav<br/>
<br/>
<img src ="https://github.com/I-Alpha/RootNav-With-OpenCV-Yolov3/blob/master/RootNav_Original.gif?raw=true">
<br/><br/>

## RootNav with Yolo-v3 implemented<br/>
<br/>
<img src ="https://github.com/I-Alpha/RootNav-With-OpenCV-Yolov3/blob/master/RootNav_withYolov3.gif?raw=true">

### Very Brief Explanation. Contact me for a more indepth explanation. 

The overarching aim of this project was to use machine learning practices to improve and/or replace the current Root Detection System in RootNav. A convolutional neural network (CNN) model, specifically YOlOv3, was trained on a dataset of 4956 images of plant roots to detect the primary and lateral tips, and the seed of the root.

Training was done using a pre-trained model on darknet and the loss was recordered. A K-Fold evaluation, where K=5, was performed on each iteration of the model. After configuring the training and part of the YOLOv3 model architectire, my model managed to reach around 85% mAp. Following evaluation, this model was frozen and, using OpenCV, a c# library, I imported the model into .Net framework and created a class library with an interface to guide the incorporation different models in the future.

The only thing I regret about this particular model/project is not having good enough dataset (need better resolution and more instances of lateral labels) for the precision I wanted to be able to offer anyone using the software. 



#### To use:
     1. Download repository into working folder/drive
     2. Download model trained .weights file. Fair warning, this file is pretty chunky so.....
        Here's the gdrive :<a href="https://drive.google.com/file/d/13eXmiIRf82l7NZqV47MFTWmvu6WpSt2h/view?usp=sharing" style="font-size:1rem">Click here</a>
     3. Once yolov3.weights file is downloaded, place it the content folder in the repository ( .....\RootNav-With-OpenCV-Yolov3\OpenCvSharpYolo3\Content).
     4. Run RootNav from .exe file or build and run in Visual Studio. You don't need to build all the modules, just RootNav and it's dependencies, including my OpenCVYOLOv3 Module 
     
#### notes: 
      There are 3 example never-seen photos of plant root structure in the contents folder, labeled 1-3.png.
      
  ##### If the openCVYOLOv3 is working, the model's detection results should appear as tip highlights in the main window, overlayed onto the plant root photo. These can then be treated the same as user inputed tip hightlights; Actions such as modifying, deleteing or adding more tips can be done as necessary by the user. Comparing this to vanilla RootNav it should be obvious what the benefits are in terms of speed of specimen processing.
  
In the future I hope to train and use a YOLOv4 object detection system to further enhance the productivity of RootNav. Or better yet, after more research, try creating my own model, optimized for RootNav and its specific requirements. 
