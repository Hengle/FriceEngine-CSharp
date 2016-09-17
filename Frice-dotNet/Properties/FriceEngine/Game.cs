﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Frice_dotNet.Properties.FriceEngine.Object;
using Frice_dotNet.Properties.FriceEngine.Utils.Graphics;

namespace Frice_dotNet.Properties.FriceEngine
{
	public class Game : Form
	{
		public Game()
		{
			_objects = new List<IAbstractObject>();
			_objectsAddBuffer = new List<IAbstractObject>();
			_objectsDeleteBuffer = new List<IAbstractObject>();
			_texts = new List<IAbstractObject>();
			_textsAddBuffer = new List<IAbstractObject>();
			_textsDeleteBuffer = new List<IAbstractObject>();
			SetBounds(100, 100, 500, 500);
			OnInit(this, EventArgs.Empty);
			Show();
			new Thread(Run).Start();
		}

		private readonly IList<IAbstractObject> _objects;
		private readonly IList<IAbstractObject> _objectsAddBuffer;
		private readonly IList<IAbstractObject> _objectsDeleteBuffer;

		private readonly IList<IAbstractObject> _texts;
		private readonly IList<IAbstractObject> _textsAddBuffer;
		private readonly IList<IAbstractObject> _textsDeleteBuffer;

		public void AddObject(IAbstractObject o) => _objectsAddBuffer.Add(o);

		public void RemoveObject(IAbstractObject o) => _objectsDeleteBuffer.Add(o);

		public event EventHandler OnInit = delegate { };

		public event EventHandler OnRefresh = delegate { };

		protected override void OnPaint(PaintEventArgs e)
		{
			HandleBuffer();

			foreach (var o in _objects)
			{
				if (o is ShapeObject)
				{
					var pen = new Pen((o as ShapeObject).ColorResource.Color);
					if ((o as ShapeObject).Shape is FRectangle)
					{
						e.Graphics.DrawRectangle(pen,
							(float) (o as ShapeObject).X,
							(float) (o as ShapeObject).Y,
							(float) (o as ShapeObject).Width,
							(float) (o as ShapeObject).Height
						);
					}
					else if ((o as ShapeObject).Shape is FOval)
					{
						e.Graphics.DrawEllipse(pen,
							(float) (o as ShapeObject).X,
							(float) (o as ShapeObject).Y,
							(float) (o as ShapeObject).Width,
							(float) (o as ShapeObject).Height
						);
					}
				}
				else if (o is ImageObject)
				{
				}
			}
			foreach (var t in _texts)
			{
			}
		}

		private void Run()
		{
			while (true)
			{
				OnRefresh(this, EventArgs.Empty);
			}
			// ReSharper disable once FunctionNeverReturns
		}

		private void HandleBuffer()
		{
			foreach (var o in _objectsAddBuffer) _objects.Add(o);
			_objectsAddBuffer.Clear();
			foreach (var o in _objectsDeleteBuffer) _objects.Remove(o);
			_objectsDeleteBuffer.Clear();

			foreach (var o in _textsAddBuffer) _texts.Add(o);
			_textsAddBuffer.Clear();
			foreach (var o in _textsDeleteBuffer) _texts.Remove(o);
			_textsDeleteBuffer.Clear();
		}
	}
}
