using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class PhysicsCreatorUpdater
{
	private class SearchQuery
	{
		public SearchQuery(string n, string t)
		{
			this.searchName = n;
			this.searchType = t;
		}

		public string searchName;
		public string searchType;
	}

	static PhysicsCreatorUpdater()
	{
		//Debug.Log("PhysicsCreatorUpdater checking for orphaned files");

		int numDeleted = 0;

		SearchQuery[] filesToRemove =
		{
			new SearchQuery("HullPainter", "t:Script"),
			new SearchQuery("HullPainterChildEditor", "t:Script"),
			new SearchQuery("HullPainterEditor", "t:Script"),
			new SearchQuery("HullPainterWindow", "t:Script"),
			new SearchQuery("HullPainterChild", "t:Script"),
			new SearchQuery("HullPainterShortcuts", "t:Script"),
			new SearchQuery("HullPainterWindow", "t:Script"),
			new SearchQuery("HullPreview", "t:Shader"),
		};

		foreach (SearchQuery query in filesToRemove)
		{
			string[] guids = AssetDatabase.FindAssets(query.searchName + " " + query.searchType);
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);

				string name = path;
				int dotPos = name.LastIndexOf('.');
				if (dotPos != -1)
				{
					name = name.Substring(0, dotPos);
				}
				int slashPos = name.LastIndexOf('/');
				if (slashPos != -1)
				{
					name = name.Substring(slashPos+1);
				}
				int backSlashPos = name.LastIndexOf('\\');
				if (backSlashPos != -1)
				{
					name = name.Substring(backSlashPos+1);
				}

				if (name == query.searchName)
				{
					// Only delete assets containing "Technie" in the path just in case a user's project also has a file with the same name
					if (path.Contains("Technie"))
					{
						//Debug.Log("Deleting: " + path);
						//AssetDatabase.DeleteAsset(path);
						numDeleted++;
					}
				}
			}
		}

		//Debug.Log("PhysicsCreatorUpdater done, deleted " + numDeleted + " files");
	}
}