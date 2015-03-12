﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace osu_automapper
{
    public enum SliderTypes
    {
        L,
        B,
        P
    }
    public enum MusicNotes
    {
        Whole,
        Half,
        Quarter,
        Eighth,
        Sixteenth
    }

    class Beatmap
    {
        // Beat map text
        // TODO: Replace string array with file?
        private string[] text;
        private string fileName;

        // Beatmap metadata.
        public int bpm { get; set; }
        public float mpb { get; set; } // Milliseconds per beat
        public float songLength { get; set; } // Length of song (in milliseconds).
        public int offset { get; set; } // Offset of the beatmap.

        // This is implying a 4/4 Time Signature. Any others might be out of scope
        private int currentBeat = 0;
        private int beatsPerMeasure = 4;
        private float sliderVelocity = 1.5f;
        private int sliderLengthPerBeat;

        // Difficulty
        private int maxNoteDistance = 100;

        public Random rnd;
        public Random rnd2;

        private Dictionary<string, float> notes = new Dictionary<string, float>();
        private char[] sliderTypes = new char[] { 'L', 'B', 'P' };

        public Beatmap(string fileName)
        {
            // TODO: Complete member initialization
            
            this.fileName = fileName;

            // Random is so bad and I'm actually furious
            int seed = (int)DateTime.Now.Ticks;
            rnd = new Random(seed);
            rnd2 = new Random((int)(rnd.Next() * (float)seed));

            Console.WriteLine("Loading .osu file..." + fileName);
            string fileRawText = System.IO.File.ReadAllText(fileName);
            string[] fileSplitText = fileRawText.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);

            Console.WriteLine(fileSplitText[4]);

            this.text = fileSplitText;

            sliderLengthPerBeat = (int)(sliderVelocity * 100 / (beatsPerMeasure));

            // Fill Dictionary
            notes.Add("whole", 4f);
            notes.Add("half", 2f);
            notes.Add("quarter", 1f);
            notes.Add("eighth", .5f);
            notes.Add("sixteenth", .25f);

            LoadBeatmapFromFile();
        }

        //TODO:
        public void GenerateBeatmapFile()
        {
            throw new NotImplementedException("TODO: Auto Generate a beatmap file from scratch. (headers, settings, etc.)");
        }

        /// <summary>
        /// Parse .osu Beatmap file.
        /// </summary>
        public void LoadBeatmapFromFile()
        {
            for (int i = 0; i < text.Length; i++)
            {
                // Get the init timing points and the milleseconds per beat.
                
                if (text[i] == "[TimingPoints]")
                {
                    string[] initTimingPonits = text[i + 1].Split(',');
                    mpb = float.Parse(initTimingPonits[1]);
                    offset = int.Parse(initTimingPonits[0]);
                }

                else if (text[i] == "[General]")
                {
                    string mp3FilePath = GetMP3FilePath(fileName, i + 1);
                    NAudio.Wave.WaveStream pcm = NAudio.Wave.WaveFormatConversionStream.CreatePcmStream(new NAudio.Wave.Mp3FileReader(mp3FilePath));
                    TimeSpan ts = pcm.TotalTime;
                    songLength = ((ts.Minutes * 60) + ts.Seconds) * 1000 + ts.Milliseconds;
                    Console.WriteLine("Total Song Length is: " + songLength);
                }

                else if (text[i] == "[Difficulty]")
                {
                    string[] sliderVelText = text[i + 5].Split(':');
                    sliderVelocity = float.Parse(sliderVelText[1]);
                    Console.WriteLine("Slider Velocity is: " + sliderVelocity);
                }
            }
        }

        // Gets the filepath for the mp3 file.
        private string GetMP3FilePath(string filePath, int lineIndex)
        {
            string[] path = filePath.Split('\\');

            // For Debug/Testing purposes. Repleace with general case
            string[] temp = text[lineIndex].Split(':');
            string mp3FileName = temp[1].Trim();

            path[path.Length - 1] = mp3FileName;
            Console.WriteLine(path[path.Length - 1]);
            string rtn = "";
            for (int i = 0; i < path.Length; i++)
            {
                rtn += path[i];
                if (i != path.Length - 1)
                    rtn += "\\";
            }

            Console.WriteLine("MP3 Path is: " + rtn);
            return rtn;
        }

        // Creates a random beatmap. This APPENDS to the current file. 
        // TODO: Need to make sure we are not overwriting other data, and starting from the correct location.
        public void CreateRandomBeatmap()
        {
            if (!File.Exists(this.fileName))
            {
                Console.WriteLine("Error: File not found.(" + this.fileName + ")");
                return;
            }

            Console.WriteLine("Generating Random Beatmap. Appending to file..." + this.fileName);

            StreamWriter osu_file = new StreamWriter(this.fileName, true);
            int numCircles = 0;

            // Basic Beatmap Creation
            Point prevPoint = new Point();
            prevPoint.X = -1; 
            prevPoint.Y = -1;

            float timestamp = (float)offset;
            // for (float timestamp = (float)offset; timestamp < songLength; timestamp += mpb)
            while (timestamp < songLength)
            {
                int x = (int)rnd.Next(10, 500);
                int y = (int)rnd.Next(10, 370);

                if (currentBeat % beatsPerMeasure == 0)
                {
                    // Generate a random slider.
                    char sliderType = sliderTypes[rnd.Next(0, 3)];
                    string hitSliderString = ReturnHitSlider(x, y, (int)timestamp, 2, 0, sliderType , 1, sliderVelocity, rnd.Next(2,4));
                    osu_file.WriteLine(hitSliderString);
                    numCircles++;
                    timestamp += AddTime("half");
                    currentBeat += (int)notes["half"];

                }

                else
                {
                    string hitCircleString = ReturnHitCircle(x, y, (int)timestamp, 1, 0, prevPoint);
                    osu_file.WriteLine(hitCircleString);
                    numCircles++;
                    timestamp += AddTime("quarter");
                    currentBeat += (int)notes["quarter"];

                    prevPoint.X = x;
                    prevPoint.Y = y;
                }
            }

            Console.WriteLine("Number of circles " + numCircles);
            osu_file.Close();
        }

        public void CreateRandomBeatmap(AudioAnalyzer analyzer)
        {
            if (!File.Exists(this.fileName))
            {
                Console.WriteLine("Error: File not found.(" + this.fileName + ")");
                return;
            }

            Console.WriteLine("Generating Random Beatmap. Appending to file..." + this.fileName);

            StreamWriter osu_file = new StreamWriter(this.fileName, true);
            int numCircles = 0;

            // Basic Beatmap Creation
            Point prevPoint = new Point();
            prevPoint.X = rnd.Next(10, 500);
            prevPoint.Y = rnd.Next(10, 370);

            float timestamp = (float)offset;
            // for (float timestamp = (float)offset; timestamp < songLength; timestamp += mpb)
            while (timestamp < songLength)
            {
                int x = 0;// (int)rnd.Next(10, 500);
                int y = 0;// (int)rnd.Next(10, 370);

                //Gets a point on a circle whose center lies at the previous point.
                while (x < 10 && x > 500 && y < 10 && y > 370)
                {
                    float radius = rnd.Next(30, maxNoteDistance);//<---- This could be a function of bpm, i.e. time-distance relation between beats
                    float dirx = (float)rnd.NextDouble();
                    float diry = (float)rnd2.NextDouble();
                    x = prevPoint.X + (int)(dirx * radius);
                    y = prevPoint.Y + (int)(diry * radius);
                }

                //@ANDREW: Look at AudioAnalyzer.CreatePeakDataAt(), and AudioAnalyzer.CreatePeakData().
                //         If you want to use music notes as you do here, use .CreatePeakDataAt().
                //         If you want to collect all the peak data at once,
                //         use .CreatePeakData() <--- but don't actually use this yet, it won't work right.
                // 
                //TODO: Now we need to consult the analyzer as we go.
                //      Since the time interval changes (halfnote vs quarter, etc), we have to 
                //      update the analyzer at the same interval as "timestamp"

                //@Andrew: It can be used like this. 
                //         Important Note: PeakData.value is in DECIBELS.
                //                         So threshold should also relate to DECIBELS.
                //       

                //EXAMPLE (NOT TESTED):
                double threshold = Double.Parse("1.0E-40");
                var peakData = analyzer.CreatePeakDataAt((int)timestamp, 450);
                Console.WriteLine(peakData.ToString());
                if (peakData.value < threshold)
                {
                    //Continue without adding a beat here if no sound was detected.
                    timestamp += AddTime("half");
                    continue;
                }
                //END EXAMPLE

                if (currentBeat % beatsPerMeasure == 0)
                {
                    // Generate a random slider.
                    char sliderType = sliderTypes[rnd.Next(0, 3)];
                    string hitSliderString = ReturnHitSlider(x, y, (int)timestamp, 2, 0, sliderType, 1, sliderVelocity, rnd.Next(2, 4));
                    osu_file.WriteLine(hitSliderString);
                    numCircles++;
                    timestamp += AddTime("half");
                    currentBeat += (int)notes["half"];
                }
                else
                {
                    string hitCircleString = ReturnHitCircle(x, y, (int)timestamp, 1, 0, prevPoint);
                    osu_file.WriteLine(hitCircleString);
                    numCircles++;
                    timestamp += AddTime("quarter");
                    currentBeat += (int)notes["quarter"];

                    prevPoint.X = x;
                    prevPoint.Y = y;
                }
            }

            Console.WriteLine("Number of circles " + numCircles);
            osu_file.Close();
        }

        private float AddTime(string note)
        {
            return notes[note] * mpb;
        }

        // Returns the string of a Hit Circle to be added to the beatmap.
        public string ReturnHitCircle(int x, int y, int time, int type, int hitSound, Point prevPoint)
        {
            HitCircle hc = new HitCircle(x, y, time, type, hitSound, prevPoint, maxNoteDistance, rnd);
            return hc.ToString();
        }

        public string ReturnHitSlider(int x, int y, int time, int type, int hitsound, char sliderType, int repeat, float sliderVelocity, int numCurves)
        {
            // Length will determine how long a slider will go on for
            int len = (int)(sliderVelocity * 100);
            HitSlider hs = new HitSlider(x, y, time, type, hitsound, sliderType, repeat, sliderVelocity, numCurves, len, rnd);
            return hs.ToString();
        }
    }
}
