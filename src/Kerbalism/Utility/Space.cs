using UnityEngine;


namespace KERBALISM
{


	// represent a 3d reference frame
	struct Space
	{
		internal Space(Vector3 x_axis, Vector3 y_axis, Vector3 z_axis, Vector3 origin, float scale)
		{
			this.x_axis = x_axis;
			this.y_axis = y_axis;
			this.z_axis = z_axis;
			this.origin = origin;
			this.scale = scale;
		}

		internal Vector3 Transform_in(Vector3 p)
		{
			p -= origin;
			p /= scale;
			return new Vector3
			(
			  Vector3.Dot(p, x_axis),
			  Vector3.Dot(p, y_axis),
			  Vector3.Dot(p, z_axis)
			);
		}

		internal Vector3 Transform_out(Vector3 p)
		{
			return origin
			  + x_axis * (p.x * scale)
			  + y_axis * (p.y * scale)
			  + z_axis * (p.z * scale);
		}

		internal Matrix4x4 Look_at()
		{
			return Matrix4x4.TRS
			(
			  origin,
			  Quaternion.LookRotation(z_axis, y_axis),
			  new Vector3(scale, scale, scale)
			);
		}

		internal Vector3 x_axis;
		internal Vector3 y_axis;
		internal Vector3 z_axis;
		internal Vector3 origin;
		internal float scale;
	}


} // KERBALISM

