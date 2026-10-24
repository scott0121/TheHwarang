using UnityEngine;

public class CurvedLinePoint : MonoBehaviour
{
	[HideInInspector] public bool showGizmo = true;
	[HideInInspector] public float gizmoSize = 0.1f;
	[HideInInspector] public Color gizmoColor = new Color(1, 0, 0, 0.5f);
	public Vector3? targetPosition = null;
	public GameObject m_targetObject = null;
	public bool useTargetObject = false;
	void OnDrawGizmos()
	{
		if (showGizmo == true)
		{
			Gizmos.color = gizmoColor;

			Gizmos.DrawSphere(this.transform.position, gizmoSize);
		}
	}

	//update parent line when this point moved
	void OnDrawGizmosSelected()
	{
		CurvedLineRenderer curvedLine = this.transform.parent.GetComponent<CurvedLineRenderer>();

		if (curvedLine != null)
		{
			curvedLine.Update();
		}
	}

	private void Update()
	{
		UpdatePosition();
	}

	public void UpdatePosition()
	{
		if (m_targetObject != null && useTargetObject)
		{
			this.transform.position = m_targetObject.transform.position;
		}
		else if (targetPosition != null && !useTargetObject)
		{
			this.transform.position = targetPosition.Value;
		}
	}
}
