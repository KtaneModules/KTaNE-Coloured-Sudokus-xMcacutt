using System.Collections;
using System.Collections.Generic;
using KModkit;
using UnityEngine;

public class ColorblindHelperScript : MonoBehaviour
{
	public TextMesh textMesh;

	public void SetFromColor(Color color)
	{
		if (color == Colors.Black)
		{
			textMesh.color = Colors.White;
			textMesh.text = "K";
		}
		if (color == Colors.Red)
		{
			textMesh.color = Colors.Black;
			textMesh.text = "R";
		}
		if (color == Colors.Green)
		{
			textMesh.color = Colors.Black;
			textMesh.text = "G";
		}
		if (color == Colors.Blue)
		{
			textMesh.color = Colors.White;
			textMesh.text = "B";
		}
		if (color == Colors.Cyan)
		{
			textMesh.color = Colors.Black;
			textMesh.text = "C";
		}
		if (color == Colors.Yellow)
		{
			textMesh.color = Colors.Black;
			textMesh.text = "Y";
		}
		if (color == Colors.Pink)
		{
			textMesh.color = Colors.Black;
			textMesh.text = "P";
		}
		if (color == Colors.Purple)
		{
			textMesh.color = Colors.White;
			textMesh.text = "V";
		}
		if (color == Colors.White)
		{
			textMesh.color = Colors.Black;
			textMesh.text = "W";
		}
		if (color == Colors.Orange)
		{
			textMesh.color = Colors.Black;
			textMesh.text = "O";
		}
		if (color == Colors.ThermoRed)
		{
			textMesh.color = Colors.White;
			textMesh.text = "R";
		}
		if (color == Colors.ThermoBlue)
		{
			textMesh.color = Colors.White;
			textMesh.text = "B";
		}
	}
}
