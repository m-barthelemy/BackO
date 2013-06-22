// NOTE: Generated code DO NOT EDIT
//
// Author: 
//	Sebastien Pouliot  <sebastien@gmail.com>
// 
// Copyright 2012 Symform Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// 'Software'), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Security.Cryptography;
using Crimson.CryptoDev;

namespace Crimson.Security.Cryptography {

	public class SHA1Kernel : SHA1 {

		HashHelper helper;


		public SHA1Kernel ()
		{
		}

		~SHA1Kernel ()
		{
			Dispose (false);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && (helper != null)) {
				helper.Dispose ();
				helper = null;
				GC.SuppressFinalize (this);
			}
			base.Dispose (disposing);
		}

		public override void Initialize ()
		{
			helper = new HashHelper (Cipher.SHA1);
		}

		protected override void HashCore (byte[] data, int start, int length) 
		{
			if (helper == null)
				Initialize ();
			helper.Update (data, start, length);
		}

		protected override byte[] HashFinal () 
		{
			if (helper == null)
				Initialize ();
			return helper.Final (HashSize >> 3);
		}
	}
}