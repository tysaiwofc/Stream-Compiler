using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace VideoStreamer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.flv";
            openFileDialog.Title = "Select a Video File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string videoPath = openFileDialog.FileName;
                Console.WriteLine("Selected video: " + videoPath);

                StartVideoStreaming(videoPath);
            }
        }

        static void StartVideoStreaming(string videoPath)
        {
            string hlsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hls_streams");

            if (!Directory.Exists(hlsDirectory))
                Directory.CreateDirectory(hlsDirectory);

            string uniqueFolderName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string newFolderPath = Path.Combine(hlsDirectory, uniqueFolderName);

            int suffix = 1;
            while (Directory.Exists(newFolderPath))
            {
                newFolderPath = Path.Combine(hlsDirectory, uniqueFolderName + "_" + suffix);
                suffix++;
            }

            Directory.CreateDirectory(newFolderPath);

            string newVideoPath = Path.Combine(newFolderPath, Path.GetFileName(videoPath));
            File.Move(videoPath, newVideoPath);
            Console.WriteLine("Video moved to: " + newVideoPath);

            string[] resolutions = { "1080p", "720p", "480p", "360p" };
            string[] scales = { "1920:1080", "1280:720", "854:480", "640:360" };

            string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_bin", "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                Console.WriteLine("FFmpeg executable not found!");
                return;
            }

            string textToAdd = "StreamCompiler";

            for (int i = 0; i < resolutions.Length; i++)
            {
                string resolution = resolutions[i];
                string scale = scales[i];
                string resolutionFolderPath = Path.Combine(newFolderPath, resolution);

                if (!Directory.Exists(resolutionFolderPath))
                    Directory.CreateDirectory(resolutionFolderPath);

                string hlsOutput = Path.Combine(resolutionFolderPath, "stream.m3u8");

                string arguments = string.Format(
                    "-i \"{0}\" -vf \"scale={1},drawtext=text='{2}':fontfile=/Windows/Fonts/arial.ttf:fontsize=24:fontcolor=white:x=w-tw-10:y=h-th-10\" " +
                    "-c:v libx264 -preset veryfast -b:v 4M -maxrate 4M -bufsize 8M " +
                    "-c:a aac -b:a 128k -hls_time 10 -hls_list_size 0 -hls_segment_filename \"{3}/segment_%03d.ts\" \"{4}\"",
                    newVideoPath, scale, textToAdd, resolutionFolderPath, hlsOutput
                );

                Process ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = ffmpegPath;
                ffmpegProcess.StartInfo.Arguments = arguments;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;

                ffmpegProcess.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                ffmpegProcess.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

                ffmpegProcess.Start();
                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();

                ffmpegProcess.WaitForExit();
            }

            MoveVideoBack(newVideoPath, videoPath);
            OpenFolderInExplorer(newFolderPath);
        }

        static void MoveVideoBack(string currentPath, string originalPath)
        {
            if (File.Exists(currentPath))
            {
                string originalDirectory = Path.GetDirectoryName(originalPath);
                if (!Directory.Exists(originalDirectory))
                {
                    Directory.CreateDirectory(originalDirectory);
                }

                File.Move(currentPath, originalPath);
                Console.WriteLine("Video moved back to original path: " + originalPath);
            }
            else
            {
                Console.WriteLine("Error: Processed video file not found at " + currentPath);
            }
        }

        static void OpenFolderInExplorer(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Process.Start("explorer.exe", folderPath);
            }
            else
            {
                Console.WriteLine("Directory " + folderPath + " not found.");
            }
        }
    }
}
