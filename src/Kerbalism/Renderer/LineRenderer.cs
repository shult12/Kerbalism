using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	// a simple line renderer
	static class LineRenderer
	{
		// pseudo-ctor
		internal static void Init()
		{
			// load shader
			mat = Lib.GetShader("AntiAliasedLine");
		}

#if DEBUG_SOLAR
		internal static void CommitWorldVector(Vector3d start, Vector3d direction, float lengthKm, Color color)
		{
			Vector3d end = ScaledSpace.LocalToScaledSpace(start + (direction * (lengthKm * 1000.0)));
			ScaledSpace.LocalToScaledSpace(ref start);
			Commit(start, end, color);
		}
#endif

		// commit a line
		static void Commit(Vector3 a, Vector3 b, Color color)
		{
			// create a new line
			Line_data line = new Line_data
			{
				a = a,
				b = b,
				color = color
			};

			// commit it
			lines.Add(line);
		}


		// render all committed lines
		internal static void Render()
		{
			// half width of line, in pixels
			const float half_width = 2.0f;

			// store stuff
			Vector3 a;            // shortcut to line start
			Vector3 b;            // shortcut to line end
			Vector3 diff;         // vector from a to b
			Vector3 v;            // perpendicular vector
			Vector3 Va;           // perpendicular vector scaled by start distance
			Vector3 Vb;           // perpendicular vector scaled by end distance
			Vector3 p0;           // bottom left point
			Vector3 p1;           // bottom right point
			Vector3 p2;           // top left point
			Vector3 p3;           // top right point

			// get camera
			var cam = PlanetariumCamera.Camera;
			Vector3 cam_pos = cam.transform.position;
			Vector3 cam_up = cam.transform.up;
			Vector3 cam_right = cam.transform.right;

			// enable the material
			mat.SetPass(0);

			// start rendering lines
			GL.Begin(GL.TRIANGLES);

			// projection factor
			// note: for some reasons, this result in a screen size 20% smaller than it should be
			float k = half_width * 2.0f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;

			// for each line we got
			foreach (Line_data line in lines)
			{
				// shortcuts
				a = line.a;
				b = line.b;

				// set the color
				GL.Color(line.color);

				// find vector perpendicular to look and line, in world space
				diff = b - a;
				v = (cam_right * -Vector3.Dot(diff, cam_up) + cam_up * Vector3.Dot(diff, cam_right)).normalized * k;

				// scale perpendicular vector to have exact pixel length
				Va = v * (a - cam_pos).magnitude;
				Vb = v * (b - cam_pos).magnitude;

				// calculate quand points
				p0 = a - Va;
				p1 = a + Va;
				p2 = b - Vb;
				p3 = b + Vb;

				// bottom left
				GL.TexCoord2(-1.0f, 0.0f);
				GL.Vertex(p0);

				// bottom right
				GL.TexCoord2(1.0f, 0.0f);
				GL.Vertex(p1);

				// top right
				GL.TexCoord2(1.0f, 0.0f);
				GL.Vertex(p3);

				// top right
				GL.TexCoord2(1.0f, 0.0f);
				GL.Vertex(p3);

				// top left
				GL.TexCoord2(-1.0f, 0.0f);
				GL.Vertex(p2);

				// bottom left
				GL.TexCoord2(-1.0f, 0.0f);
				GL.Vertex(p0);
			}

			// stop rendering triangles
			GL.End();

			// clear all committed lines
			lines.Clear();
		}


		// store a committed line
		class Line_data
		{
			internal Vector3 a;       // starting point
			internal Vector3 b;       // ending point
			internal Color color;     // line color
		};

		// set of committed lines
		static List<Line_data> lines = new List<Line_data>(32);

		// materials used for rendering
		static Material mat;
	}


} // KERBALISM
