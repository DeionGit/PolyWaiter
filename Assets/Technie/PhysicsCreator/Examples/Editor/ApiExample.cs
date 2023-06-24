using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Technie.PhysicsCreator;
using Technie.PhysicsCreator.Rigid;

/*
 *	An example script to show how to interact with the Technie Collider Creator api to batch generate colliders for lots of objects at once.
 *
 *	Usage:
 *		Open a scene, add your models you want to add collision to
 *		Select them all in the hierarchy
 *		Run this script from the menus (Window->Technie Collider Creator->Run API Example)
 *		Save the scene or convert your objects into prefabs to use elsewhere
 *		
 *	Cavets:
 *		This is an example use of the API, it is not nessesarily a production-ready script. It is deliberately kept simple to be easy to
 *		learn the basics and be used as a foundation for further work.
 *		
 *		The API is a work-in-progress and subject to change, there will be sharp corners which will be improved in future versions. If you
 *		have any feedback or suggestions please let me know in the discord support channel (link in the documentation)
 *
 *	Notes:
 *		When scripting the editor you probably want to do things in a coroutine so you can have long-running operations that don't stall
 *		the editor for the user. You can use the EditorCoroutines class to run a coroutine in the editor without needing a MonoBehaviour.
 *		
 *		At the moment the Collider Creator tool assumes you're working on the currently selected hierarchy object. This isn't ideal and will
 *		be addressed in future updates. For now it's advised to set the selection yourselve before doing any API calls.
 *
 *		Most common operations have a keyboard shortcut for them - you can find these functions in Shortcuts.cs, and is a good place to start
 *		looking for functions you might want to call yourself. For rigid collider creation you'll also want to look in the  RigidColliderCreatorWindow
 *		and SceneManipulator classes.
 *
 *
 */
public class ApiExample
{
	[MenuItem("Window/Technie Collider Creator/Run API Example (Bulk Collider Generate)", false, 2000)]
	public static void RunApiExample()
	{
		EditorCoroutines.Execute(ApiExampleRoutine());
	}
	
	private static IEnumerator ApiExampleRoutine()
	{
		// Get the currently selected objects that we want to work on
		GameObject[] objects = Selection.gameObjects;

		// Open the creator window
		RigidColliderCreatorWindow.ShowWindow();
		RigidColliderCreatorWindow rigidWindow = RigidColliderCreatorWindow.instance;

		// Loop over the target objects
		foreach (GameObject targetObj in objects)
		{
			// Find the mesh components on the target object
			MeshFilter filter = targetObj.GetComponent<MeshFilter>();
			MeshRenderer renderer = targetObj.GetComponent<MeshRenderer>();

			if (filter != null && renderer != null)
			{
				Debug.Log("Api example processing " + targetObj.name);

				// Set the hierarchy selection to the object we're processing
				Selection.activeGameObject = targetObj;

				// Generate the asset data which stores the painting data and hull data. These will be named based on the mesh's name
				PaintingData paintingData = RigidColliderCreatorWindow.GenerateAsset(targetObj, filter.sharedMesh);

				// By default painting data comes with one hull already created. We'll delete it as we want to make our own
				Debug.Log("Deleting starter hull");
				rigidWindow.DeleteActiveHull();
				
				// Set the auto collider quality level to 'high' (this applies to all hulls on this object)
				paintingData.autoHullPreset = AutoHullPreset.High;
		
				// Add a new hull and set it to 'auto' hull generation
				Hull hull = rigidWindow.AddHull();
				hull.type = HullType.Auto;

				// This hull should contain all the faces so we generate colliders for the whole mesh
				rigidWindow.SceneManipulator.PaintAllFaces();

				// Now we've configured the painting data, calculate the hulls and create collider components for them
				rigidWindow.GenerateColliders();

				// Generating colliders is a long running operation (with a progress bar) so we'll loop here until it's done
				do
				{
					yield return null;
				}
				while (rigidWindow.IsGeneratingColliders);

				// We've finished this object - on to the next one!
				Debug.Log("Api example generated colliders for " + targetObj.name);
			}
			else
			{
				// We couldn't find the mesh components on this object so we'll skip it
				Debug.Log(string.Format("Skipping {0} because it doesn't have a MeshFilter and MeshRenderer on it", targetObj.name), targetObj);
			}
		}

		// Close the window now we're done with it
		rigidWindow.Close();

		// Restore the selection to what it was before
		Selection.objects = objects;

		Debug.Log("Api Example finished");
	}
}
