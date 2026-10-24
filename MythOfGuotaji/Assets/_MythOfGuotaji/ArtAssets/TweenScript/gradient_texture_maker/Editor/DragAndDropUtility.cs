#if UNITY_2021_2_OR_NEWER

using Packages.GradientTextureGenerator.Runtime;
using UnityEditor;
using UnityEngine;

namespace Packages.GradientTextureGenerator.Editor
{

	public static class DragAndDropUtility
	{
#if UNITY_6000_0_OR_NEWER
		static DragAndDrop.ProjectBrowserDropHandlerV2 _handlerProject;
#else
		static DragAndDrop.ProjectBrowserDropHandler _handlerProject;
#endif

		[InitializeOnLoadMethod]
		public static void Init()
		{
			_handlerProject = ProjectDropHandler;
#if UNITY_6000_0_OR_NEWER
			DragAndDrop.RemoveDropHandlerV2(_handlerProject);
			DragAndDrop.AddDropHandlerV2(_handlerProject);
#else
			DragAndDrop.RemoveDropHandler(_handlerProject);
			DragAndDrop.AddDropHandler(_handlerProject);
#endif
		}

#if UNITY_6000_0_OR_NEWER
		private static DragAndDropVisualMode ProjectDropHandler(EntityId dragEntityId, string dropUponPath, bool perform)
#else
		private static DragAndDropVisualMode ProjectDropHandler(int dragInstanceId, string dropUponPath, bool perform)
#endif
		{
			if (!perform)
			{
				var dragged = DragAndDrop.objectReferences;
				bool found = false;
				for (var i = 0; i < dragged.Length; i++)
				{
					if (dragged[i] is GradientTexture gradient)
					{
						dragged[i] = gradient.GetTexture();
						found = true;
					}
				}
				if (found)
				{
					DragAndDrop.objectReferences = dragged;
					GUI.changed = true;
					return default;
				}

			}
			return default;
		}
	}
}

#endif