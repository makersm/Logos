﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Forms;
using Microsoft.Samples.Kinect.WpfViewers;
using Microsoft.Kinect.Toolkit.Interaction;
using System.Runtime.InteropServices;
using PreHands.PPT;
using PreHands.MouseControl;



[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UI-WPF", Justification = "Sample is correctly named")]
namespace PreHands
{
    using System.Windows.Input;
    using System.Windows.Media;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows.Data;
    using System.Windows.Media.Media3D;

    using Microsoft.Kinect.Toolkit;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow
    {
        private InteractionStream _interactionStream;

        Skeleton_eventHandler eventCreaterPPT = new Skeleton_eventHandler();
        //Skeleton_eventHandler eventChecker = new Skeleton_eventHandler();
        public static bool isPPTActive = false;
        public static bool isMouseActive = true;

        public MainWindow()
        {
            this.InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject())) return;

            //eventChecker.registEventListener += new Skeleton_eventHandler.customEvent(ppt_eventLists.startPPTControl);
            eventCreaterPPT.registEventListener += new Skeleton_eventHandler.customEvent(ppt_eventLists.startPPT);
            eventCreaterPPT.registEventListener += new Skeleton_eventHandler.customEvent(ppt_eventLists.SlideChecker);
            eventCreaterPPT.registEventListener += new Skeleton_eventHandler.customEvent(ppt_eventLists.endPPT);
        }

        bool closing = false;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);

        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;

            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null)
            {
                return;
            }

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };
            sensor.SkeletonStream.Enable(parameters);

                      //sensor.SkeletonStream.Enable();

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            _interactionStream = new InteractionStream(sensor, new MDKinectInteractions());
            _interactionStream.InteractionFrameReady += its_InteractionFrameReady;

            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }


        private void OnShowSettings(object sender, MouseEventArgs e)
        {
            this.setting_title.Visibility = Visibility.Collapsed;
            this.setting_img.Visibility = Visibility.Collapsed;
            this.SettingsControl.Visibility = Visibility.Visible;


        }

        public void OnHideSettings(object sender, MouseEventArgs e)
        {
            this.setting_title.Visibility = Visibility.Visible;
            this.setting_img.Visibility = Visibility.Visible;
            this.SettingsControl.Visibility = Visibility.Collapsed;
        }
        //THIS IS THE REAL ACTS LIKE MAIN PART; WE HAVE TO FIX THIS.
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            //Get a skeleton
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }

            //regist events
            checkEvent(first);

            //set scaled position
            //ScalePosition(headImage, first.Joints[JointType.Head]);
            ScalePosition(leftEllipse, first.Joints[JointType.HandLeft]);
            ScalePosition(rightEllipse, first.Joints[JointType.HandRight]);

            GetCameraPoint(first, e);

            setCursor(first);

            //interactionStream에 skeleton과 depth 등록
            setInteraction(e);

        }

        //interactionStream에 skeleton과 depth 등록
        private void setInteraction(AllFramesReadyEventArgs e)
        {
            KinectSensor _sensor = kinectSensorChooser1.Kinect;

            if (_sensor == null)
            {
                return;
            }
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {

                if (skeletonFrame == null)
                    return;

                try
                {
                    skeletonFrame.CopySkeletonDataTo(allSkeletons);
                    var accelerometerReading = _sensor.AccelerometerGetCurrentReading();
                    _interactionStream.ProcessSkeleton(allSkeletons, accelerometerReading, skeletonFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // SkeletonFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                    return;

                try
                {
                    _interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // DepthFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
        }
        private void setCursor(Skeleton first)
        {
            //마우스 이동 부분
            if (isMouseActive)
            {
                CursorEvent.cursorMove(first);
            }  
        }

        //핸드포인터 이벤트
        private void its_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            if (isMouseActive)
            {
                CursorEvent.cursorAction(e);
            }    
        }

        //Button Click
        void hoverButtonRight_Click(object sender, RoutedEventArgs e)
        {
            // ppt mode 실행 시 Window 창 최소화 시킴
            this.WindowState = WindowState.Minimized;
            e.Handled = true;

        }

        void hoverButtonRight_Click2(object sender, RoutedEventArgs e)
        {
            // Mouse mode 실행 시 Window 창 최소화 시킴
            this.WindowState = WindowState.Minimized;
            e.Handled = true;
        }

        void hoverButtonRight_Click3(object sender, RoutedEventArgs e)
        {
            // Recoding 실행 시 Window 창 최소화 시킴
            this.WindowState = WindowState.Minimized;
            e.Handled = true;
        }

        /// Called when the OnLoaded storyboard completes.
        private void OnLoadedStoryboardCompleted(object sender, System.EventArgs e)
        {
            var parent = (Canvas)this.Parent;
            parent.Children.Remove(this);
        }




        void checkEvent(Skeleton first)
        {
            //eventChecker.start(new Skeleton_eventArgs(first));
            if (isPPTActive)
            {
                eventCreaterPPT.start(new Skeleton_eventArgs(first));
            }
        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }

                //Map a joint location to a point on the depth map
                //head
                DepthImagePoint headDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.Head].Position);
                //left hand
                DepthImagePoint leftDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);
                //right hand
                DepthImagePoint rightDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);

                //Map a depth point to a point on the color image
                //head
                ColorImagePoint headColorPoint =
                    depth.MapToColorImagePoint(headDepthPoint.X, headDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left hand
                ColorImagePoint leftColorPoint =
                    depth.MapToColorImagePoint(leftDepthPoint.X, leftDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right hand
                ColorImagePoint rightColorPoint =
                    depth.MapToColorImagePoint(rightDepthPoint.X, rightDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //Set location : that handle hand, head image in view
                //CameraPosition(headImage, headColorPoint);
                CameraPosition(leftEllipse, leftColorPoint);
                CameraPosition(rightEllipse, rightColorPoint);


            }
        }


        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }


                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;

            }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }


                }
            }
        }
   
        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - (element.Width / 2));
            Canvas.SetTop(element, point.Y - (element.Height / 2));

        }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 

            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);

        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            StopKinect(kinectSensorChooser1.Kinect);
        }
    }
}
