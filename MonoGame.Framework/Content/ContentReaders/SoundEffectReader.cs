// #region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
// #endregion License
// 

using System;
using System.IO;

using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework.Content
{
	internal class SoundEffectReader : ContentTypeReader<SoundEffect>
	{
#if ANDROID
        static string[] supportedExtensions = new string[] { ".wav", ".mp3", ".ogg", ".mid" };
#else
        static string[] supportedExtensions = new string[] { ".wav", ".aiff", ".ac3", ".mp3" };
#endif

        internal static string Normalize(string fileName)
        {
            return Normalize(fileName, supportedExtensions);
        }

		protected internal override SoundEffect Read(ContentReader input, SoundEffect existingInstance)
		{                    
			byte[] header = input.ReadBytes(input.ReadInt32());
			byte[] data = input.ReadBytes(input.ReadInt32());
			int loopStart = input.ReadInt32();
			int loopLength = input.ReadInt32();
			int num = input.ReadInt32();

            byte[] soundData = null;
            // Proper use of "using" corectly disposes of BinaryWriter which in turn disposes the underlying stream
            MemoryStream mStream = new MemoryStream(20 + header.Length + 8 + data.Length);
            using (BinaryWriter writer = new BinaryWriter(mStream))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write((int)(20 + header.Length + data.Length));
                writer.Write("WAVE".ToCharArray());

                //header can be written as-is
                writer.Write("fmt ".ToCharArray());
                writer.Write(header.Length);
                writer.Write(header);

                writer.Write("data".ToCharArray());
                writer.Write((int)data.Length);
                writer.Write(data);

                // Copy the data to an array before disposing the stream
                soundData = mStream.ToArray();
            }
            if (soundData == null)
                throw new ContentLoadException("Failed to load SoundEffect");
			return new SoundEffect(input.AssetName, soundData);
		}
	}
}
