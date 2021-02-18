using System;
using System.Collections.Generic;
using System.IO;
using FFMediaToolkit;
using FFMediaToolkit.Decoding;

namespace Mp3Test {

   
    public class Extraction : IDisposable, IEnumerator<Tuple<byte[], int>> {
      static Extraction() {
        FFmpegLoader.FFmpegPath = "../../../../Lib";
      }

      public Extraction(string filename) {
        media_file_ = MediaFile.Open(filename);
        Console.WriteLine("Opened media file {0} with format {1}, starting timeline at {2}", filename, media_file_.Info.ContainerFormat, StartTime);
      }

      public void Reset() {

      }

      public bool HasAudio => media_file_?.HasAudio ?? false;

      public int SampleRate => media_file_.Audio.Info.SampleRate;

      public DateTimeOffset StartTime {
        get {
          var creation_timestamp = media_file_.Info.Metadata.Metadata.GetValueOrDefault("creation_time");
          if (creation_timestamp == null || !DateTimeOffset.TryParse(creation_timestamp, out DateTimeOffset start_time)) {
            Console.WriteLine("Unable to use metadata creation time {0}, falling back to file creation time {1}", creation_timestamp, media_file_.Info.FileInfo.CreationTimeUtc);
            start_time = media_file_.Info.FileInfo.CreationTimeUtc;
          }
          return start_time;
        }
      }

      public TimeSpan TimePosition => media_file_.Audio.Position;

      public Tuple<byte[], int> Current => Tuple.Create(linear_bytes_, available_bytes_);
      object System.Collections.IEnumerator.Current => linear_bytes_;

      int FetchNextChunk() {
        int offset = 0;
        if (media_file_.Audio != null) {
          while (offset < linear_bytes_.Length && TimePosition < end_limit_) {
            try {
              if (!media_file_.Audio.TryGetNextFrame(out FFMediaToolkit.Audio.AudioData frame)) {
                Console.WriteLine("No more frames available at {0}", TimePosition);
                break;
              }

              var channel = frame.GetChannelData(0);
              for (int i = 0; i < channel.Length; ++i) {
                var a = (short)(channel[i] * short.MaxValue);
                linear_bytes_[offset++] = (byte)(a & 0xFF);
                linear_bytes_[offset++] = (byte)(a >> 8);
              }
              frame.Dispose();

              // break earlier if buffer doesn't seem to have enough date for next frame
              if (linear_bytes_.Length - offset < channel.Length * 2)
                break;

            } catch (EndOfStreamException) {
              Console.WriteLine("Encountered end of audio stream");
              break;
            } catch (Exception e) {
              Console.WriteLine("Encountered error when processing audio: {0}", e.Message);
            }
          }
        }
        return offset;
      }

      public void Dispose() {
        if (media_file_ != null)
          media_file_.Dispose();
      }

      public bool MoveNext() {
        return (available_bytes_ = FetchNextChunk()) > 0;
      }

      readonly byte[] linear_bytes_ = new byte[1 << 20];
      int available_bytes_;
      TimeSpan end_limit_ = TimeSpan.MaxValue;
      readonly MediaFile media_file_;
    }


  class Program {
    static void Process(string filename) {
      using var e = new Extraction(filename);
      while (e.MoveNext())
        Console.WriteLine("chunk with size {0}", e.Current.Item2);
    }

    static void Main(string[] args) {
      if (args.Length < 1)
        Console.WriteLine("Usage: Mp3Test.exe file.mp3");
      Process(args[0]);
      Console.WriteLine("Processed, existing");
    }
  }
}
