# RootNav_OpencvYolov3

## Very Brief Explanation. Contact me for a more indepth explanation. 

The overarching aim of this project was to use machine learning practices to improve and/or replace the current Root Detection System in RootNav. A convolutional neural network (CNN) model, specifically YOlOv3, was trained on a dataset of 4956 images of plant roots to detect the primary and lateral tips, and the seed of the root.

Training was done using a pre-trained model on darknet and the loss was recordered. A K-Fold evaluation, where K=5, was performed on each iteration of the model. After configuring the training and part of the YOLOv3 model architectire, my model managed to reach around 85% mAp. Following evaluation, this model was frozen and, using OpenCV, a c# library, I imported the model into .Net framework and created a class library with an interface to guide the incorporation different models in the future.

The only thing I regret about this particular model/project is not having good enough dataset (need better resolution and more instances of lateral labels) for the precision I wanted to be able to offer anyone using the software. 



### To use:
     1. download repository into working folder/drive
     2. download model trained .weights file from link :
     3. place model into .....\RootNav-With-OpenCV-Yolov3\OpenCvSharpYolo3\Content folder.
     4. Run RootNav from .exe file or build and run in Visual Studio. 
     
### notes: 
      There are 3 example never-seen photos of plant root structure in the contents folder, labeled 1-3.png.
      
  ### If the openCVYOLOv3 is working, you should expect to see the model process and label tips in an instant after a plant root photo has been loaded into RootNav. These tips can then be treated as normal RootNav label tips, modifying, deleteing or adding more as necessary to the workflow of the user.    
  
In the future I hope to train and use a YOLOv4 object detection system to further enhance the productivity of RootNav. 
