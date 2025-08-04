using System.Collections;
using System.Collections.Generic;
using KModkit;
using UnityEngine;

public class ColorblindHelperScript : MonoBehaviour
{
	public TextMesh textMesh;

	public void SetFromColor(Color color, string text = null)
	{
		if (color == Colors.Black)
		{
			textMesh.color = Colors.White;
			textMesh.text = text ?? "";
		}
		else if (color == Colors.Red)
		{
			textMesh.color = Colors.Black;
			textMesh.text = text ?? "R";
		}
		else if (color == Colors.Green)
		{
			textMesh.color = Colors.Black;
			textMesh.text = text ?? "G";
		}
		else if (color == Colors.Blue)
		{
			textMesh.color = Colors.White;
			textMesh.text = text ?? "B";
		}
		else if (color == Colors.Cyan)
		{
			textMesh.color = Colors.Black;
			textMesh.text = text ?? "C";
		}
		else if (color == Colors.Yellow)
		{
			textMesh.color = Colors.Black;
			textMesh.text = text ?? "Y";
		}
		else if (color == Colors.Pink)
		{
			textMesh.color = Colors.Black;
			textMesh.text = text ?? "P";
		}
		else if (color == Colors.Purple)
		{
			textMesh.color = Colors.White;
			textMesh.text = text ?? "V";
		}
		else if (color == Colors.White)
		{
			textMesh.color = Colors.Black;
			textMesh.text = text ?? "W";
		}
		else if (color == Colors.Orange)
		{
			textMesh.color = Colors.Black;
			textMesh.text = text ?? "O";
		}
		else if (color == Colors.ThermoRed)
		{
			textMesh.color = Colors.White;
			textMesh.text = text ?? "R";
		}
		else if (color == Colors.ThermoBlue)
		{
			textMesh.color = Colors.White;
			textMesh.text = text ?? "B";
		}
	}
}
