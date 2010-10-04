/** Copyright (c) 2006, All-In-One Creations, Ltd.
*  All rights reserved.
* 
* Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
* 
*     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
*     * Neither the name of All-In-One Creations, Ltd. nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
* 
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
/**
 * Project: emergetk: stateful web framework for the masses
 * File name: Image.cs
 * Description: A framework level data type used for Image-based widgets.
 *   
 * Author: Ben Joldersma
 *   
 * @see The GNU Public License (GPL)
 */
/* 
 * This program is free software; you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation; either version 2 of the License, or 
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License along 
 * with this program; if not, write to the Free Software Foundation, Inc., 
 * 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */

using System;
using EmergeTk.Widgets.Html;

namespace EmergeTk.Model
{
	/// <summary>
	/// Summary description for Image.
	/// </summary>
	public class ImageRecord : AbstractRecord
	{
		private string url;
		
		/// <summary>
		/// Property Url (string)
		/// </summary>
		public string Url
		{
			get
			{
				return this.url;
			}
			set
			{
				this.url = value;
			}
		}
		
		public new int Version {
			get {
				return version;
			}
			set {
				version = value;
			}
		}
		public bool SaveToLocalStorage { get; set; }
		
		int version = 0;

        private string thumbnailUrl;

        public string ThumbnailUrl
        {
            get { return thumbnailUrl; }
            set { thumbnailUrl = value; }
        }

        public override Widget GetEditWidget(Widget parent, ColumnInfo column, IRecordList records)
        {
			EnsureId();
			ImageUpload iu = Context.Current.CreateWidget<ImageUpload>();
			iu.SavePath = url;
			iu.ImageUid = Version.ToString();
			iu.SaveToLocalStorage = SaveToLocalStorage;
			System.IO.Directory.CreateDirectory( System.Web.HttpContext.Current.Server.MapPath( "/Storage" ) );
			iu.SaveFormat = string.Format("/Storage/{0}.$Extension", this.Id );
			if (SaveToLocalStorage)
			{
				iu.OnImageUploaded += new EventHandler(delegate(object o, EventArgs ea)
				{
					Url = iu.SavePath;
					iu.ImageUid = (++Version).ToString();
					Save();
				});
			}
			return iu;
		}

		public ImageRecord()
		{
			SaveToLocalStorage = true;
		}
	}
}
