using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Amilious.Core.Editor {
	[CustomEditor(typeof(Readme))]
	[InitializeOnLoad]
	public class ReadmeEditor : UnityEditor.Editor {
	
		static string kShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";
	
		static ReadmeEditor()
		{
			EditorApplication.delayCall += SelectReadmeAutomatically;
		}

		private static void SelectReadmeAutomatically() {
			if(SessionState.GetBool(kShowedReadmeSessionStateName, false)) return;
			var readme = SelectReadme();
			SessionState.SetBool(kShowedReadmeSessionStateName, true);
			if(!readme || readme.loadedLayout) return;
			LoadLayout();
			readme.loadedLayout = true;
		}

		private static void LoadLayout() {
			var assembly = typeof(EditorApplication).Assembly; 
			var windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", true);
			var method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static);
			method?.Invoke(null, new object[]{Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"), false});
		}
	
		[MenuItem("Tutorial/Show Tutorial Instructions")]
		private static Readme SelectReadme() {
			var ids = AssetDatabase.FindAssets("Readme t:Readme");
			if (ids.Length == 1) {
				var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));
				Selection.objects = new []{readmeObject};
				return (Readme)readmeObject;
			}else {
				Debug.Log("Couldn't find a readme");
				return null;
			}
		}
	
		protected override void OnHeaderGUI() {
			var readme = (Readme)target;
			Init();
			var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth/3f - 20f, 128f);
			GUILayout.BeginHorizontal("In BigTitle"); {
				GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
				GUILayout.Label(readme.title, TitleStyle);
			}
			GUILayout.EndHorizontal();
		}
	
		public override void OnInspectorGUI() {
			var readme = (Readme)target;
			Init();
			var usingInline = false;
		
			foreach (var section in readme.sections) {
				//start inline
				if(!usingInline && section.inLine) {
					usingInline = true;
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
				}
				//end inline
				if(usingInline && !section.inLine) {
					usingInline = false;
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}
				GUILayout.BeginVertical();
				GUILayout.Space(section.spaceBefore);
				GUILayout.FlexibleSpace();
				if (!string.IsNullOrEmpty(section.heading)) {
					BeginAlign(section.alignment);
					GUILayout.Label(section.heading, HeadingStyle);
					EndAlign(section.alignment);
				}
				if(section.icon != null) {
					var iconWidth = (section.iconWidth > 0) ? section.iconWidth : section.icon.width;
					iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth- 20f, iconWidth);
					var iconHeight = section.icon.height * iconWidth / section.icon.height;
					BeginAlign(section.alignment);
					GUILayout.Label(section.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconHeight));
					EndAlign(section.alignment);
				}
				if (!string.IsNullOrEmpty(section.text)) {
					BeginAlign(section.alignment);
					GUILayout.Label(section.text, BodyStyle);
					EndAlign(section.alignment);
				}
				if (!string.IsNullOrEmpty(section.linkText)) {
					BeginAlign(section.alignment);
					if (LinkLabel(new GUIContent(section.linkText))) {
						Application.OpenURL(section.url);
					}
					EndAlign(section.alignment);
				}
				GUILayout.Space(section.spaceAfter);
				GUILayout.EndVertical();
			}
			//start inline
			if(!usingInline) return;
			GUILayout.EndHorizontal();
		}

		private void BeginAlign(Readme.ReadmeAlignment alignment) {
			GUILayout.BeginHorizontal();
			if(alignment == Readme.ReadmeAlignment.Center|| 
			   alignment == Readme.ReadmeAlignment.Right) 
				GUILayout.FlexibleSpace();
		}

		private void EndAlign(Readme.ReadmeAlignment alignment) {
			if(alignment == Readme.ReadmeAlignment.Center|| 
			   alignment == Readme.ReadmeAlignment.Left) 
				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
	
	
		bool m_Initialized;
	
		GUIStyle LinkStyle { get { return m_LinkStyle; } }
		[SerializeField] GUIStyle m_LinkStyle;
	
		GUIStyle TitleStyle { get { return m_TitleStyle; } }
		[SerializeField] GUIStyle m_TitleStyle;
	
		GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
		[SerializeField] GUIStyle m_HeadingStyle;
	
		GUIStyle BodyStyle { get { return m_BodyStyle; } }
		[SerializeField] GUIStyle m_BodyStyle;
	
		void Init()
		{
			if (m_Initialized)
				return;
			m_BodyStyle = new GUIStyle(EditorStyles.label);
			m_BodyStyle.wordWrap = true;
			m_BodyStyle.fontSize = 14;
		
			m_TitleStyle = new GUIStyle(m_BodyStyle);
			m_TitleStyle.fontSize = 26;
		
			m_HeadingStyle = new GUIStyle(m_BodyStyle);
			m_HeadingStyle.fontSize = 18 ;
		
			m_LinkStyle = new GUIStyle(m_BodyStyle);
			m_LinkStyle.wordWrap = false;
			// Match selection color which works nicely for both light and dark skins
			m_LinkStyle.normal.textColor = new Color (0x00/255f, 0x78/255f, 0xDA/255f, 1f);
			m_LinkStyle.stretchWidth = false;
		
			m_Initialized = true;
		}
	
		bool LinkLabel (GUIContent label, params GUILayoutOption[] options) {
			var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

			Handles.BeginGUI ();
			Handles.color = LinkStyle.normal.textColor;
			Handles.DrawLine (new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
			Handles.color = Color.white;
			Handles.EndGUI ();

			EditorGUIUtility.AddCursorRect (position, MouseCursor.Link);

			return GUI.Button (position, label, LinkStyle);
		}
	}
}

