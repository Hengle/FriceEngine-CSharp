﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using FriceEngine.Animation;
using FriceEngine.Resource;
using FriceEngine.Utils.Graphics;
using FriceEngine.Utils.Misc;

namespace FriceEngine.Object
{
	public interface IAbstractObject
	{
		double X { get; set; }
		double Y { get; set; }
		int Uid { get; }
		double Rotate { get; set; }
	}

	public class AbstractObject : IAbstractObject
	{
		public double X { get; set; }
		public double Y { get; set; }
		public double Rotate { get; set; }
		public int Uid { get; }
		public AbstractObject(double x, double y)
		{
			X = x;
			Y = y;
		}
	}

	public interface IFContainer
	{
		double Width { get; set; }
		double Height { get; set; }
	}

	public interface ICollideBox
	{
		bool IsCollide(ICollideBox other);
	}

	public abstract class PhysicalObject : IAbstractObject, IFContainer, ICollideBox
	{
		public virtual double X { get; set; }
		public virtual double Y { get; set; }

		public virtual double Width { get; set; }
		public virtual double Height { get; set; }

		public abstract int Uid { get; }
		public double Rotate { get; set; } = 0;

		public bool Died { get; set; } = false;

		private double _mass = 1;

		public double Mass
		{
			get { return _mass; }
			set { _mass = value <= 0 ? 0.001 : value; }
		}

		public abstract bool IsCollide(ICollideBox other);
	}

	public abstract class FObject : PhysicalObject
	{
		protected FObject()
		{
			MoveList = new ConcurrentDictionary<int, MoveAnim>();
			TargetList = new List<Pair<PhysicalObject, Action>>();
		}

		public override int Uid { get; } = StaticHelper.GetNewUid();
		public ConcurrentDictionary<int,MoveAnim> MoveList { get; }
		public List<Pair<PhysicalObject, Action>> TargetList { get; }

		public void Move(double x, double y)
		{
			X += x;
			Y += y;
		}

		public void Move(DoublePair p) => Move(p.X, p.Y);

		/// <summary>
		/// handle all animations
		/// </summary>
		public void RunAnims()
		{
			MoveAnim ma;
			MoveList.Keys.ToList().ForEach(i =>
			{
				MoveList.TryGetValue(i, out ma);
				if (ma != null)
				{
					Move(ma.Delta);
				}
			});
		}
		/// <summary>
		/// Add animations
		/// </summary>
		public void AddAnims(params MoveAnim[] ma)
		{
			foreach (MoveAnim moveAnim in ma)
			{
				MoveList.TryAdd(moveAnim.Uid,moveAnim);
			}
			
		}
		/// <summary>
		/// Remove animations
		/// </summary>
		public void RemoveAnims(params MoveAnim[] ma)
		{
			foreach (MoveAnim moveAnim in ma)
			{
				MoveAnim m;
				MoveList.TryRemove(moveAnim.Uid, out m);
			}
		}
		/// <summary>
		/// Clear animations
		/// </summary>
		public void ClearAnims()
		{
			MoveList.Clear();
		}

		/// <summary>
		/// check all collition targets
		/// </summary>
		public void CheckCollitions()
		{
			TargetList.RemoveAll(p => p.First.Died);
			foreach (var p in TargetList.Where(p => IsCollide(p.First)))
				p.Second.Invoke();
		}

		/// <summary>
		/// check if two collideBoxes are collided.
		/// if 'other' isn't a PhysicalObject, it will always return false;
		/// </summary>
		/// <param name="other">the other collide box</param>
		/// <returns>collided or not.</returns>
		public override bool IsCollide(ICollideBox other)
		{
			if (other is PhysicalObject)
				return X + Width >= ((PhysicalObject) other).X && ((PhysicalObject) other).Y <= Y + Height &&
						X <= ((PhysicalObject) other).X + ((PhysicalObject) other).Width &&
						Y <= ((PhysicalObject) other).Y + ((PhysicalObject) other).Height;
			return false;
		}

		public bool ContainsPoint(double px, double py) => px >= X && px <= X + Width && py >= Y && py <= Y + Height;
		public bool ContainsPoint(int px, int py) => px >= X && px <= X + Width && py >= Y && py <= Y + Height;
	}

	public sealed class ShapeObject : FObject
	{
		public IFShape Shape;
		public ColorResource ColorResource;

		public override double Width
		{
			get { return Shape.Width; }
			set { Shape.Width = value; }
		}

		public override double Height
		{
			get { return Shape.Height; }
			set { Shape.Height = value; }
		}

		public ShapeObject(ColorResource colorResource, IFShape shape, double x, double y)
		{
			ColorResource = colorResource;
			Shape = shape;
			X = x;
			Y = y;
		}

		public ShapeObject(Color color, IFShape shape, double x, double y) :
			this(new ColorResource(color), shape, x, y)
		{
		}

		public ShapeObject(int argb, IFShape shape, double x, double y) :
			this(new ColorResource(argb), shape, x, y)
		{
		}
	}

	public sealed class ImageObject : FObject
	{
		public ImageResource Res { get; set; }

		public Bitmap Bitmap
		{
			get { return Res.Bmp; }
			set { Res.Bmp = value; }
		}

		public Point Point { get; set; }
		private double _x;
		private double _y;

		public override double X
		{
			get { return _x; }
			set
			{
				_x = value;
				Point = new Point(Convert.ToInt32(_x), Convert.ToInt32(_y));
			}
		}

		public override double Y
		{
			get { return _y; }
			set
			{
				_y = value;
				Point = new Point(Convert.ToInt32(_x), Convert.ToInt32(_y));
			}
		}

		public override double Height
		{
			get { return Bitmap.Height; }
			set { Bitmap = _resize(Bitmap, Convert.ToInt32(Width), Convert.ToInt32(value)); }
		}

		public override double Width
		{
			get { return Bitmap.Width; }
			set { Bitmap = _resize(Bitmap, Convert.ToInt32(value), Convert.ToInt32(Height)); }
		}


		public ImageObject(ImageResource img, double x, double y)
		{
			Res = img;
			_x = x;
			_y = y;
			Point = new Point(Convert.ToInt32(_x), Convert.ToInt32(_y));
		}

		public ImageObject(Bitmap img, double x, double y) :
			this(new ImageResource(img), x, y)
		{
		}

		public static ImageObject FromWeb(string url, double x, double y, int width = -1, int height = -1)
		{
			var r = WebRequest.Create(url).GetResponse() as HttpWebResponse;
			using (var imageStream = r?.GetResponseStream())
			{
				var img = imageStream == null ? null : new ImageObject(new Bitmap(imageStream, true), x, y);
				if (width > 0 && img != null) img.Width = width;
				if (height > 0 && img != null) img.Height = height;
				return img;
			}
		}

		/// <summary>
		/// 感谢ifdog老司机！
		/// </summary>
		/// <param name="oldBitmap">the original bitmap</param>
		/// <param name="newW">new bitmap width</param>
		/// <param name="newH">new bitmap height</param>
		/// <returns>scaled bitmap</returns>
		/// <author>ifdog</author>
		private static Bitmap _resize(Image oldBitmap, int newW, int newH)
		{
			var b = new Bitmap(newW, newH);
			using (var g = Graphics.FromImage(b))
			{
				g.Clear(Color.Transparent);
				g.CompositingQuality = CompositingQuality.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.DrawImage(oldBitmap, new Rectangle(0, 0, newW, newH), 0, 0,
					oldBitmap.Width,
					oldBitmap.Height,
					GraphicsUnit.Pixel);
				return b;
			}
		}

		/// <summary>
		/// 感谢ifdog老司机！
		/// </summary>
		/// <param name="path">image path</param>
		/// <param name="x">position x</param>
		/// <param name="y">position y</param>
		/// <param name="width">image width, defaultly original size.</param>
		/// <param name="height">image height, defaultly original size.</param>
		/// <returns></returns>
		public static ImageObject FromFile(string path, double x, double y, int width = -1, int height = -1)
		{
			var img = new ImageObject(new Bitmap(path, true), x, y);
			if (width > 0) img.Width = width;
			if (height > 0) img.Height = height;
			return img;
		}

		public ImageObject Clone()
		{
			return  new ImageObject(Res.Bmp.Clone() as Bitmap, X,Y);
		}
	}


	public class DoublePair
	{
		public double X;
		public double Y;

		public DoublePair(double y, double x)
		{
			Y = y;
			X = x;
		}

		public static DoublePair From1000(double x, double y) => new DoublePair(x / 1000.0, y / 1000.0);

		public static DoublePair FromKilo(double x, double y) => From1000(x, y);

		public static DoublePair FromTicks(long x, long y) => new DoublePair(x / 1e7, y / 1e7);
		public static DoublePair FromTicks(double x, double y) => new DoublePair(x / 1e7, y / 1e7);
	}
}
