using System;
using System.IO;
using BorrehSoft.Utensils.Log;

namespace recover550
{
	class MainClass
	{
		public enum RecoveryState : int {
			Nothing = 0,
			Header = 1,
			Body1 = 2,
			Body2 = 3,
			Body3 = 4,
			Body4 = 5,
		}

		public const int MB = 1048576;

		public static void Main (string[] args)
		{
			// cannibalized
		    // https://github.com/strandtentje/apollogeese/blob/master/Bootloader/Head.cs
			string time = DateTime.Now.ToString ("yyyy-MM-dd--THHmmsszz");
			Secretary logger = new Secretary (string.Format ("{0}.log", time));
			logger.globVerbosity = 6;
			logger.ReportHere (0, "Logfile Opened");

			Stream stdin = Console.OpenStandardInput ();
			// Stream stdin = File.OpenRead ("TEST.CR2");

			RecoveryState state = RecoveryState.Nothing;

			byte[] header = new byte[12];
			byte[] closer = new byte[] {
				0xff, 0xd9
			};

			int headercursor = 0;
			int closercursor = 0;

			using (FileStream data = File.OpenRead("SIGNATURE.CR2")) {
				data.Read (header, 0, 12);
			}

			Secretary.Report (5, "Header bytes:", header.Length.ToString());
			Secretary.Report (5, "Footer bytes:", closer.Length.ToString());

			FileStream outfile = null;
			int filecounter = 0;
			int filesize = 0;
			long totalbytes = 0;
			
			int readSig = stdin.ReadByte();

			while (-1 < readSig) {
				byte inByte = (byte)readSig;

				if (header[headercursor] == inByte) {
					headercursor++;
					if (state == RecoveryState.Nothing) {
						Secretary.Report (8, "Nothing->Header");
						state = RecoveryState.Header;
					}
				} else {
					headercursor = 0;
					if (state == RecoveryState.Header) {
						Secretary.Report (8, "Header->Nothing");
						state = RecoveryState.Nothing;
					}
				}

				if (headercursor == header.Length) {
					headercursor = 0;
					state = RecoveryState.Body1;
					Secretary.Report (5, "Header->Body1");
					filecounter++;
					string filename = string.Format ("REC_{0}.CR2", filecounter.ToString ("D8"));
					outfile = File.OpenWrite (filename);
					Secretary.Report(5, "Start writing to", filename);
					// -1 here because we writebyte later.
					outfile.Write (header, 0, header.Length - 1);
					filesize = header.Length;
				}

				switch (state) {
				case RecoveryState.Body1:
				case RecoveryState.Body2:
				case RecoveryState.Body3:
					outfile.WriteByte (inByte);
					filesize++;
					if (filesize % MB == 0)
						Secretary.Report (5, "write", state.ToString (), "@", (filesize / MB).ToString(), "MB");
					if (closer [closercursor] == inByte) {
						closercursor++;
						if (closercursor == closer.Length) {
							closercursor = 0;
							state++;
						}
					} else {
						closercursor = 0;
					}
					break;
				case RecoveryState.Body4:
					outfile.WriteByte (inByte);
					filesize++;
					if (filesize % MB == 0)
						Secretary.Report (5, "write", state.ToString (), "@", (filesize / MB).ToString(), "MB");
					if (closer [closercursor] == inByte) {
						closercursor++;
						if (closercursor == closer.Length) {
							closercursor = 0;
							state = RecoveryState.Nothing;
							filesize = 0;
							outfile.Close ();
						}
					} else {
						closercursor = 0;
					}
					break;
				default:
					break;
				}

				totalbytes++;

				if (totalbytes % (MB * 100) == 0) {
					Secretary.Report (5, "read", (totalbytes / MB).ToString(), "MB from disk");
				}

				readSig = stdin.ReadByte();
			}
		}
	}
}
