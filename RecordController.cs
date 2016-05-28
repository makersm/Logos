﻿/* @Auther Jaeung Choi(darker826, darker826@gmail.com)
* 녹화 기능을 담당하는 클래스입니다.
* 객체를 생성 후 함수를 부르기만 하면 사용이 가능함.
* .avi 동영상 파일의 생성에는 AForge.Video.FFMPEG wrapper를 사용하여 FFMPEG 라이브러리를 통해 생성합니다.
* 동영상 파일의 프레임은 KinectColorViewer에서 WriteableBitmap를 Bitmap으로 Convert하여 Bitmap을 넣는 것으로 처리합니다.
*/


namespace Microsoft.Samples.Kinect.WpfViewers
{
    using System;
    using System.IO;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Windows.Forms;
    using System.Windows.Media.Imaging;
    using System.Runtime.InteropServices;

    using Microsoft.Kinect;
    using AForge.Video.FFMPEG;

    public class RecordController
    {
        private static VideoFileWriter writer;
        private static Timer timer;
        private static bool testBool = true;
        private static bool recordNow = false;


        public RecordController()
        {
            writer = new VideoFileWriter();
        }

        //타이머 작동시 불리는 함수
        /*public static void timerEvent(object sender, EventArgs e)
        {
            Console.WriteLine("타이머 한번 불렸음");
            writer.Close();
            timer.Stop();
        }*/

        //녹화 상태를 바꿔주는 함수.
        public void recordControl(ColorImageFrame colorImage, WriteableBitmap writeBmp)
        {
            if (recordNow)
            {
                recordingStop();
            }
            else
            {
                recordingStart(colorImage, writeBmp);
            }
        }

        //녹화 시작할 때 부를 함수.
        public void recordingStart(ColorImageFrame colorImage, WriteableBitmap writeBmp)
        {
            /*
            //타이머 셋팅
            if (timer == null)
            {
                timer = new Timer();
                timer.Enabled = true;
                timer.Interval = 10000;
                timer.Tick += new EventHandler(timerEvent);
            }
            */
            //        Bitmap bmap = colorFrameToBitmap(colorImage);
            Bitmap bmap = bitmapFromWriteableBitmap(writeBmp);
            /*
            if (testBool)
            {
                testBool = false;
                timer.Start();
                

                writer.Open("test.avi", colorImage.Width, colorImage.Height, 25, VideoCodec.MPEG4);
            }
            */
            recordNow = true;

            String fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".avi";
            writer.Open(fileName, colorImage.Width, colorImage.Height, 25, VideoCodec.MPEG4);

            writer.WriteVideoFrame(bmap);
        }
        
        //녹화 중지할 때 부를 함수.
        public void recordingStop()
        {
            writer.Close();
            recordNow = false;
        }

        //colorImageFrame을 받아서 Bitmap으로 변환해주는 함수.
        /*
        private Bitmap colorFrameToBitmap(ColorImageFrame colorImage)
        {
            Bitmap bmap;
            BitmapData bmapdata;
            byte[] pixeldata;
            pixeldata = new byte[colorImage.PixelDataLength];
            colorImage.CopyPixelDataTo(pixeldata);
            bmap = new Bitmap(colorImage.Width, colorImage.Height, PixelFormat.Format24bppRgb);
            bmap.SetPixel(colorImage.Width-1, colorImage.Height-1, Color.Red);
            bmapdata = bmap.LockBits(
            new System.Drawing.Rectangle(0, 0, colorImage.Width, colorImage.Height),
            ImageLockMode.WriteOnly,
            bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(pixeldata, 0, ptr, bmapdata.Stride * bmap.Height);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }
        */

        //기존에 쓰이던 WriteableBitmap을 bitmap으로 convert해주는 함수
        public Bitmap bitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }
    }
}
