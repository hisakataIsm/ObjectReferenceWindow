using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace  Ism
{
	/// <summary>
	/// Unity Objectの参照を表示するウインドウ
	/// </summary>
	public class ObjectReferenceWindow : EditorWindow
	{
		/// <summary>
		/// Git Log
		/// </summary>
		[System.Serializable]
		public class GitLog
		{
			public string commit;
			public string abbreviated_commit;
			public string refs;
			public string subject;
			public GitLogAuthor author;
			public GitLogCommiter commiter;

			/// <summary>
			/// Json形式用のフォーマット
			/// </summary>
			public static string JsonFormat()
			{
				var format = "";
				format +=	"--pretty=format:\"{";
				format +=		"\\\"commit\\\":\\\"%H\\\",";
				format +=		"\\\"abbreviated_commit\\\":\\\"%h\\\",";
				format +=		"\\\"refs\\\":\\\"%D\\\",";
				format +=		"\\\"subject\\\":\\\"%s\\\",";
				format +=		"\\\"author\\\":{";
				format +=			"\\\"name\\\":\\\"%an\\\",";
				format +=			"\\\"email\\\":\\\"%ae\\\",";
				format +=			"\\\"date\\\":\\\"%ad\\\"},";
				format +=		"\\\"commiter\\\":{";
				format +=			"\\\"name\\\":\\\"%cn\\\",";
				format +=			"\\\"email\\\":\\\"%ce\\\",";
				format +=			"\\\"date\\\":\\\"%cd\\\"}";
				format +=			"}\"";
				return format;
			}
		}

		/// <summary>
		/// Git Log Author
		/// </summary>
		[System.Serializable]
		public class GitLogAuthor
		{
			public string name;
			public string email;
			public string date;
		}

		/// <summary>
		/// Git Log Commiter
		/// </summary>
		[System.Serializable]
		public class GitLogCommiter
		{
			public string name;
			public string email;
			public string date;
		}

		/// <summary>
		/// TypeObjectInfo
		/// </summary>
		public class TypeObjectInfo
		{
			public Object typeObject;
			public bool isGitLogError;
			public GitLog gitLog;
		}

		/// <summary>
		/// Git検索の進捗
		/// </summary>
		public enum GitSearchState
		{
			None,
			Search,
			Finish,
		}

		/// <summary>
		/// メニューから開く
		/// </summary>
		[MenuItem("Ism/ObjectReferenceWindow")]
		private static void ShowWindow()
		{
			var window = GetWindow<ObjectReferenceWindow>();
			window.titleContent = new GUIContent("ObjectReferenceWindow");
			window.Show();
		}

		/// <summary>
		/// GUIのスクロール座標
		/// </summary>
		private Vector2 m_scrollPosition = Vector2.zero;

		/// <summary>
		/// 検索するオブジェクト
		/// </summary>
		private Object m_object = null;

		/// <summary>
		/// Gitを使用するかどうか
		/// </summary>
		private bool m_useGit = false;

		/// <summary>
		/// TypeObjectの数
		/// </summary>
		private int m_typeObjectNum = 0;

		/// <summary>
		/// Gitログの数 *検索が終わると1つ増える
		/// </summary>
		private int m_gitLogCount = 0;

		/// <summary>
		/// Git検索状況
		/// </summary>
		private GitSearchState m_gitSearchState = GitSearchState.None;

		/// <summary>
		/// GUIのTypeごとの開閉
		/// </summary>
		private Dictionary<System.Type, bool> m_foldouts = new Dictionary<System.Type, bool>();

		/// <summary>
		/// TypeObject情報
		/// </summary>
		private Dictionary<System.Type, List<TypeObjectInfo>> m_typeObjectInfos = new Dictionary<System.Type, List<TypeObjectInfo>>();

		/// <summary>
		/// Update
		/// </summary>
		private void Update()
		{
			// Gitログ検索中は進捗を更新するためにRepaintを行う
			if(m_gitSearchState == GitSearchState.Search)
			{
				Repaint();
			}
		}

		/// <summary>
		/// OnGUI
		/// </summary>
		private void OnGUI()
		{
			m_object = (Object)EditorGUILayout.ObjectField("Object", m_object, typeof(Object), true);
			EditorGUILayout.Space();

			if(m_object != null)
			{
				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button("Search", GUILayout.Width(120)))
				{
					SearchObject();
				}
				if(GUILayout.Button("Clipboard", GUILayout.Width(120)))
				{
					CopyClipboard();
				}
				EditorGUILayout.EndHorizontal();

				GUIGitSetting();

				GUITypeObjects();
			}
		}

		/// <summary>
		/// Objectの情報を検索する
		/// </summary>
		private void SearchObject()
		{
			m_typeObjectInfos.Clear();
			m_typeObjectNum = 0;
			m_gitSearchState = GitSearchState.None;

			SearchObjectReference(m_object);

			if(m_useGit)
			{
				EditorCoroutineUtility.StartCoroutineOwnerless(GitLogTypeObjects());
			}
		}

		/// <summary>
		/// TypeObjectの参照を検索する
		/// </summary>
		private void SearchObjectReference(Object searchObject)
		{
			if(searchObject != null)
			{
				var type = searchObject.GetType();
				if(!m_typeObjectInfos.ContainsKey(type))
				{
					m_typeObjectInfos.Add(type, new List<TypeObjectInfo>());
				}
				var typeObjectInfo = m_typeObjectInfos[type].Find(info => info.typeObject == searchObject);
				if(typeObjectInfo == null)
				{
					var info = new TypeObjectInfo()
					{
						typeObject = searchObject,
					};
					m_typeObjectInfos[type].Add(info);
					m_typeObjectNum++;
				}
				else
				{
					return;
				}

				var so = new SerializedObject(searchObject);
				var iter = so.GetIterator();
				while(iter.Next(true))
				{
					if(iter.propertyType == SerializedPropertyType.ObjectReference)
					{
						if(iter.objectReferenceValue != null)
						{
							SearchObjectReference(iter.objectReferenceValue);
						}
					}
				}
			}
		}

		/// <summary>
		/// 全てのTypeObjectのGitログを取得する
		/// </summary>
		private IEnumerator GitLogTypeObjects()
		{
			m_gitSearchState = GitSearchState.Search;
			m_gitLogCount = 0;
			foreach(var typeObjectInfo in m_typeObjectInfos)
			{
				foreach(var info in typeObjectInfo.Value)
				{
					GitLogTypeObject(info.typeObject, (json) => {
						info.gitLog = JsonUtility.FromJson<GitLog>(json);
						info.isGitLogError = false;
					}, () => {
						info.isGitLogError = true;
					});
					m_gitLogCount++;
					yield return null;
				}
			}
			m_gitSearchState = GitSearchState.Finish;
		}

		/// <summary>
		/// TypeObjectのGitログを取得する
		/// </summary>
		private void GitLogTypeObject(Object typeObject, System.Action<string> onComplete, System.Action onError)
		{
			var dataPath = Application.dataPath;
			var assetPath = AssetDatabase.GetAssetPath(typeObject).Replace("Assets/", "");
			var arguments = string.Format("-C {0} log -n 1 --date=iso {1} -- {2}", dataPath, GitLog.JsonFormat(), assetPath);

			try
			{
				// process経由でgitコマンドを実行する
				var process = new System.Diagnostics.Process();
				process.StartInfo.FileName = "git";
				process.StartInfo.Arguments = arguments;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardInput = false;
				process.StartInfo.CreateNoWindow = true;
				process.Start();

				var json = process.StandardOutput.ReadToEnd();
				if(string.IsNullOrEmpty(json))
				{
					onError?.Invoke();
				}
				else
				{
					onComplete?.Invoke(json);
				}

				process.WaitForExit();
				process.Close();
			}
			catch
			{
				onError?.Invoke();
			}
		}

		/// <summary>
		/// クリップボードにコピーする
		/// </summary>
		private void CopyClipboard()
		{
			string clip = "";
			foreach(var type in m_typeObjectInfos.Keys)
			{
				clip += string.Format("{0}\n", type.ToString());
				foreach(var typeObjectInfo in m_typeObjectInfos[type])
				{
					clip += string.Format("\t{0}", typeObjectInfo.typeObject.name.ToString());
					if(m_useGit)
					{
						if (typeObjectInfo.isGitLogError)
						{
							clip += string.Format("\t{0}", typeObjectInfo.gitLog.commiter.date);
							clip += string.Format("\t{0}", typeObjectInfo.gitLog.commiter.name);
							clip += string.Format("\t{0}", typeObjectInfo.gitLog.subject);
							clip += string.Format("\t{0}", typeObjectInfo.gitLog.commit);
						}
						else
						{
							clip += string.Format("\tGitError");
						}
					}
					clip += "\n";
				}
			}
			GUIUtility.systemCopyBuffer = clip;
		}

		/// <summary>
		/// GUI Git設定の表示
		/// </summary>
		private void GUIGitSetting()
		{
			EditorGUILayout.BeginHorizontal();
			m_useGit = EditorGUILayout.Toggle(m_useGit, GUILayout.Width(15));
			EditorGUILayout.LabelField("UseGit");
			EditorGUILayout.EndHorizontal();
			if(m_useGit)
			{
				switch (m_gitSearchState)
				{
				default:
				case GitSearchState.None:
					EditorGUILayout.LabelField("Git -");
					break;
				case GitSearchState.Search:
					EditorGUILayout.LabelField(string.Format("Git Search {0}/{1}", m_gitLogCount, m_typeObjectNum));
					break;
				case GitSearchState.Finish:
					EditorGUILayout.LabelField(string.Format("Git Finish {0}", m_gitLogCount));
					break;
				}
			}
		}

		/// <summary>
		/// GUI TypeObjectの表示
		/// </summary>
		private void GUITypeObjects()
		{
			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

			foreach(var typeObjectInfo in m_typeObjectInfos)
			{
				var type = typeObjectInfo.Key;
				var infos = typeObjectInfo.Value;
				if(!m_foldouts.ContainsKey(type))
				{
					m_foldouts.Add(type, false);
				}
				m_foldouts[type] = EditorGUILayout.Foldout(m_foldouts[type], string.Format("{0} ({1})", type, infos.Count));
				if(m_foldouts[type])
				{
					EditorGUI.indentLevel++;
					foreach(var info in infos)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.ObjectField(info.typeObject, typeof(Object), true, GUILayout.Width(250));
						if(m_useGit)
						{
							if (info.isGitLogError)
							{
								EditorGUILayout.LabelField("GitError");
							}
							else
							{
								EditorGUILayout.TextField(info.gitLog.commiter.date, GUILayout.Width(145));
								EditorGUILayout.TextField(info.gitLog.commiter.name, GUILayout.Width(150));
								EditorGUILayout.TextField(info.gitLog.subject, GUILayout.Width(200));
								EditorGUILayout.TextField(info.gitLog.commit, GUILayout.Width(100));
							}
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorGUI.indentLevel--;
				}
			}

			EditorGUILayout.EndScrollView();
		}
	}
}