// NOTE: Generated code DO NOT EDIT
//
// Author: Sebastien Pouliot  <sebastien@gmail.com>
// See LICENSE for copyrights and restrictions
//
using System;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

using Mono.Security.Cryptography;
using Crimson.CryptoDev;

namespace Crimson.Security.Cryptography {

	public class AesKernel : Aes {
		
		const int BufferBlockSize = Int32.MaxValue;

		public AesKernel ()
		{
		}
		
		public override void GenerateIV ()
		{
			IVValue = KeyBuilder.IV (BlockSizeValue >> 3);
		}
		
		public override void GenerateKey ()
		{
			KeyValue = KeyBuilder.Key (KeySizeValue >> 3);
		}
	
		RijndaelManaged Fallback ()
		{
			RijndaelManaged r = new RijndaelManaged ();
			r.Mode = Mode;
			r.Padding = Padding;
			return r;
		}
	
		public override ICryptoTransform CreateDecryptor (byte[] rgbKey, byte[] rgbIV) 
		{
			try {
				switch (Mode) {
				case CipherMode.CBC:
					return new CryptoDevTransform (this, Cipher.AES_CBC, false, rgbKey, rgbIV, BufferBlockSize);
				case CipherMode.ECB:
					return new CryptoDevTransform (this, Cipher.AES_ECB, false, rgbKey, rgbIV, BufferBlockSize);
				}
			}
			catch (CryptographicException) {
				// the kernel might not have the required mode (even for 'generic') available
			}
			// other modes, effectivelty CFB, will be implemented on top
			// on ECB, one block at the time
			return Fallback ().CreateDecryptor (rgbKey, rgbIV);
		}
		
		public override ICryptoTransform CreateEncryptor (byte[] rgbKey, byte[] rgbIV) 
		{
			try {
				switch (Mode) {
				case CipherMode.CBC:
					return new CryptoDevTransform (this, Cipher.AES_CBC, true, rgbKey, rgbIV, BufferBlockSize);
				case CipherMode.ECB:
					return new CryptoDevTransform (this, Cipher.AES_ECB, true, rgbKey, rgbIV, BufferBlockSize);
				}
			}
			catch (CryptographicException) {
				// the kernel might not have the required mode (even for 'generic') available
			}
			// other modes, effectivelty CFB, will be implemented on top
			// on ECB, one block at the time
			return Fallback ().CreateEncryptor (rgbKey, rgbIV);
		}
	}
}