using System;
using UnityEngine;

namespace Amilious.Core {
	public class Readme : ScriptableObject {
		public Texture2D icon;
		public string title;
		public Section[] sections;
		public bool loadedLayout;
	
		[Serializable]
		public class Section {
			public ReadmeAlignment alignment;
			public string heading, text, linkText, url;
			public Texture2D icon;
			public float iconWidth = -1;
			public bool inLine;
			public float spaceBefore = 0f;
			public float spaceAfter= 5f;
		}
		
		public enum ReadmeAlignment{Left, Center, Right}
	}
}
