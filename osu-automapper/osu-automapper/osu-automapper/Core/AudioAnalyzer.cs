﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace osu_automapper
{
	public class AudioAnalyzer
	{
		private WaveStream pcm;
		private Beatmap beatmap;
		private List<PeakData> peakData;

		private int bpm;
		private int offset;//in milliseconds
		private double offsetInSeconds;

		private int bytesPerSecond;

		//Convert BPM to bytesPerInterval
		//bps = bpm / 60            
		private float bps;
		private int interval;

		private int index;
		private int current;

		//TODO: Add various beatmap settings to constructor
		public AudioAnalyzer(WaveStream pcm, Beatmap beatmap)
		{
			this.pcm = pcm;
			this.beatmap = beatmap;
			Setup();
		}

		//Setups everything that CreatePeakData() and CreatePeakDataAt() will need.
		private void Setup()
		{
			peakData = new List<PeakData>();

			bpm = beatmap.bpm;

			offsetInSeconds = (double)offset / 1000.0;

			bytesPerSecond = this.pcm.WaveFormat.Channels *
								 this.pcm.WaveFormat.SampleRate *
								 this.pcm.WaveFormat.BitsPerSample / 8;

			//Convert BPM to bytesPerInterval
			//bps = bpm / 60            
			bps = ((float)bpm) / 60f;
			interval = (int)((float)bytesPerSecond * bps);

			Console.WriteLine("Debug:bmp/bps:" + bpm + "/" + bps);
			Console.WriteLine("Debug:byte interval:" + interval);

			index = (int)(offsetInSeconds * bytesPerSecond);
			current = index;
		}

		//Creates PeakData from the array of pcm data
		private PeakData CalculatePeak(int index, int time, byte[] buffer)
		{
			//For calculating peaks.            
			double sum = 0;
			for (var i = 0; i < buffer.Length; i = i + 2)
			{
				double sample = BitConverter.ToInt16(buffer, i) / 32768.0;
				sum += (sample * sample);
			}

			double rms = Math.Sqrt(sum / (buffer.Length / 2));

			var decibel = 20 * Math.Log10(rms);

			Console.Write("sum:" + sum);
			Console.Write("rms:" + rms);
			Console.WriteLine(decibel);

			return new PeakData(index, time, decibel);
		}

		/// <summary>
		/// This creates all the peak data at once, sampling at a constant interval
		/// calculated from bpm. The data is stored in List<PeakData> peakData.
		/// This is NOT flexible, peak data will be limited to the bpm interval.
		/// </summary>
		public List<PeakData> CreatePeakData()
		{
			Setup();

			byte[] buffer = new byte[interval];
			peakData = new List<PeakData>();
			this.pcm.Position = index;

			int ret = 0;
			do
			{
				ret = this.pcm.Read(buffer, 0, interval);//.Read automatically increments stream buffer index

				peakData.Add(CalculatePeak(current, current, buffer));

				current += interval;
			} while (ret != -1);

			return this.peakData;
		}

		//TODO: "range" is a very relative value. We will have to experiment with values
		//      to determine a good range. However, this value will most likely be dependant on the
		//      mp3 file, due to the possiblity of multiple channels.
		/// <summary>
		/// This moves the stream index to the current millisecond, and reads a range
		/// of values surrounding it for calculating a peak.
		/// This IS flexible, we can read bytes from any interval we want.
		/// </summary>
		/// <param name="currentMillisecond">The current millisecond in the song.</param>
		/// <param name="range">The range around the millisecond to take an average of.</param>
		public PeakData CreatePeakDataAt(int currentMillisecond, int range)
		{
			double offsetInSeconds = (double)currentMillisecond / 1000.0;
			int index = (int)(offsetInSeconds * bytesPerSecond);

			int size = (index + range) - (index - range);
			byte[] buffer = new byte[size];

			/*
			Console.Write("CMS:" + currentMillisecond);
			Console.Write(" OIS:" + offsetInSeconds);
			Console.Write(" Index:" + index);
			Console.Write(" BPS:" + bytesPerSecond);
			Console.WriteLine(" Size:" + size);
			*/

			this.pcm.Position = index - range;
			int ret = this.pcm.Read(buffer, 0, buffer.Length);
			Console.WriteLine("Ret:" + ret);
			PeakData pd = CalculatePeak(index, currentMillisecond, buffer);
			//peakData.Add(pd);
			return pd;
		}

		//This function will create a set of PeakData that is based on amplitude thresh holds,
		//completely ignoring bpm.
		//
		//Dynamic Thresh Hold Creation: The required amplitude threshold will be relative to the 
		//                              current average amplitude at that point in the song.
		//Slope Detection: This will be used to detect possible slider placement and length.
		//                 e.g. a slope close to zero across several samples 
		//                      would suggest a flat, drawn out sound (I think?). 
		//                      However, this will never truely occur because most songs will
		//                      have multiple instruments playing at the same time.
		//                      So a drawn out violin sound would be surrounded by bass "wiggles"/waves.

		public void PerformRandomBeatDetection()
		{
			throw new NotImplementedException();
		}
	}
}
