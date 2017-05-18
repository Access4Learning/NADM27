using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Draw
{
    class Node
    {
        string name;
        string type;

        public Rectangle Rect;
        public Rectangle RectEx;

        string minOccurs = "1";
        string maxOccurs = "1";

        public Dictionary<string, string> properties = new Dictionary<string, string>();

        public Node(string name, string type)
        {
            this.name = name;
            this.type = type;
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public string Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }

        public string MinOccurs
        {
            get { return minOccurs; }
            set { minOccurs = value; }
        }

        public string MaxOccurs
        {
            get { return maxOccurs; }
            set { maxOccurs = value; }
        }
    }

    class Attribute : Node
    {
        bool required = false;

        public Attribute(string name, string type) : base(name, type)
        {

        }

        public Attribute(string name, string type, bool required) : base(name, type)
        {
            this.required = required;
        }

        public bool Required
        {
            get
            {
                return required;
            }

            set
            {
                required = value;
            }
        }
    }

    enum ContentModel
    {
        SEQUENCE,
        CHOICE,
        MIXED,
        ALL
    };

    class Element : Node
    {
        Attribute[] attributes = null;
		public ArrayList children = new ArrayList();

        ContentModel contentModel = ContentModel.SEQUENCE;

        public Element(string name, string type)
            : base(name, type)
        {

        }

        public Element(string name, string type, Attribute[] attributes)
            : base(name, type)
        {
            this.attributes = attributes;
        }

        public Element(string name, string type, Attribute[] attributes, Element[] children)
            : base(name, type)
        {
            this.attributes = attributes;

			foreach (Element child in children)
			{
				this.children.Add(child);
			}
        }

        public Attribute[] Attributes
        {
            get
            {
                return attributes;
            }

            set
            {
                attributes = value;
            }
        }

        public Element[] Children
        {
            get
            {
                return (Element[])children.ToArray(typeof(Element));
            }

            set
            {
				children.Clear();

				foreach (Element child in value)
				{
					this.children.Add(child);
				}
			}
        }

        public ContentModel ContentModel
        {
            get
            {
                return contentModel;
            }

            set
            {
                contentModel = value;
            }
        }
    }

    class Diagrammer
    {
        /*
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run();
        }
         */

        Bitmap bitmap;
        Graphics graphics;

        Font nameFont;
        Font typeFont;

        Brush nameBrush;
        Brush typeBrush;

        Font attributeIconFont;
        Brush attributeIconBrush;

        Pen boxPen;
        Brush nameBGBrush;
        Brush typeBGBrush;
        Brush attributesBGBrush;

        Pen attributesPen;
        Pen linePen;

        Bitmap elementLogo;

        Font symbolFont;
        Brush symbolBrush;
        Pen symbolPen;
        Brush symbolBGBrush;

        StringFormat centered;
        StringFormat left;

        Font relationshipFont;
        Brush relationshipBrush;

        Brush bgBrush;

        Pen testPen;

        int childrenSpacing = 75;
        int attributeSpacing = 15;
        int elementSpacing = 15;

        int internalPadding = 5;
        int attributesPadding = 10;

        int relationshipWidth = 100;
        int relationshipHeight = 40;

        public Diagrammer()
        {
            Initialize();
        }

        void Initialize()
        {
            Resize(100, 100);

            nameFont = new Font("Arial", 12, FontStyle.Bold);
            typeFont = new Font("Arial", 8, FontStyle.Regular);
            attributeIconFont = new Font("Arial", 11, FontStyle.Bold);
            relationshipFont = typeFont;

            bgBrush = new SolidBrush(Color.White);
            nameBrush = new SolidBrush(Color.Black);
            typeBrush = new SolidBrush(Color.Black);
            relationshipBrush = typeBrush;
            attributeIconBrush = new SolidBrush(Color.FromArgb(0x33, 0x66, 0x99));

            boxPen = new Pen(Color.Gray, 1);
            nameBGBrush = new SolidBrush(Color.White);
            typeBGBrush = new SolidBrush(Color.LightGray);
            attributesBGBrush = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0xEE));

            attributesPen = new Pen(Color.FromArgb(0x33, 0x66, 0x99), 1);
            linePen = new Pen(Color.FromArgb(0x33, 0x66, 0x99), 1);

            symbolFont = new Font("Arial", 11, FontStyle.Bold);
            symbolBrush = new SolidBrush(Color.FromArgb(0x33, 0x66, 0x99));
            symbolPen = new Pen(Color.FromArgb(0x33, 0x66, 0x99), 1);
            symbolBGBrush = new SolidBrush(Color.LightGray);

            elementLogo = new Bitmap(@"resources\element_logo.gif");

            centered = new StringFormat(StringFormatFlags.NoClip);
            centered.Alignment = StringAlignment.Center;
            centered.LineAlignment = StringAlignment.Center;

            left = new StringFormat(StringFormatFlags.NoClip);
            left.Alignment = StringAlignment.Near;
            left.LineAlignment = StringAlignment.Near;

            testPen = new Pen(Color.Red, 1);
        }

        void Resize(int width, int height)
        {
            bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        Rectangle DrawCount(int x, int y, string minOccurs, string maxOccurs)
        {
            return DrawCount(x, y, minOccurs, maxOccurs, false);
        }

        Rectangle DrawCount(int x, int y, string minOccurs, string maxOccurs, bool measure)
        {
            Rectangle result = new Rectangle(x, y, 0, 0);

            if (minOccurs != "1" || maxOccurs != "1")
            {
                string symbol = "?";

                if (minOccurs == "0" && (maxOccurs == "unbounded" || Convert.ToInt32(maxOccurs) > 1))
                {
                    symbol = "*";

                    if (maxOccurs != "unbounded")
                    {
                        symbol = "0-" + maxOccurs;
                    }
                }
                else if (minOccurs == "1" && (maxOccurs == "unbounded" || Convert.ToInt32(maxOccurs) > 1))
                {
                    symbol = "+";

                    if (maxOccurs != "unbounded")
                    {
                        symbol = "1-" + maxOccurs;
                    }
                }

                return DrawCount(x, y, symbol, measure);
            }

            return result;
        }

        Rectangle DrawCount(int x, int y, string symbol, bool measure)
        {
            Rectangle result = new Rectangle(x, y, 0, 0);

            if (symbol != "")
            {
                SizeF symbolSize = graphics.MeasureString(symbol, symbolFont);

                int dimension = Math.Max((int)symbolSize.Width, (int)symbolSize.Height);

                result = new Rectangle(x, y, dimension, dimension);

                if (!measure)
                {
                    graphics.FillEllipse(symbolBGBrush, result);
                    graphics.DrawString(symbol, symbolFont, symbolBrush, result, centered);
                    graphics.DrawEllipse(boxPen, result);
                }
            }

            return result;
        }

        Rectangle[] DrawNode(int x, int y, Node node)
        {
            return DrawNode(x, y, node, false);
        }

        Rectangle[] DrawNode(int x, int y, Node node, bool measure)
        {
			if (node.Name == "" && node.Type == "")
			{
				if (node is Element)
				{
					Element element = node as Element;

					if (element.ContentModel == ContentModel.CHOICE)
					{
						node.Type = "(choice)";
					}
					else if (element.ContentModel == ContentModel.SEQUENCE)
					{
						node.Type = "(sequence)";
					}
					else
					{
						return new Rectangle[2] { new Rectangle(x, y, 0, 0), new Rectangle() };
					}
				}
				else
				{
					return new Rectangle[2] { new Rectangle(x, y, 0, 0), new Rectangle() };
				}
			}

            Rectangle[] result = new Rectangle[2];
            result[1] = new Rectangle();

            SizeF nameSize = graphics.MeasureString(node.Name, nameFont);
            SizeF typeSize = graphics.MeasureString(node.Type, typeFont);

            Rectangle symbolResult = DrawCount(x, y, node.MinOccurs, node.MaxOccurs, true);

            int logoWidth = elementLogo.Width;

            if (node is Attribute)
            {
                SizeF attributeIconSize = graphics.MeasureString("@", attributeIconFont);
                logoWidth = (int)attributeIconSize.Width;
            }

            int width = Math.Max((int)nameSize.Width + logoWidth, (int)typeSize.Width);
            int height = (int)(nameFont.Height + typeFont.Height);

            result[0] = new Rectangle(x, y, width + 10, height + 10);

            if (!measure)
            {
                graphics.FillRectangle(nameBGBrush, result[0]);

                if (!(node is Attribute))
                {
                    graphics.DrawImage(elementLogo, x + internalPadding, y + internalPadding);
                }
                else
                {
                    graphics.DrawString("@", attributeIconFont, attributeIconBrush, x + internalPadding, y + internalPadding - 4);
                }

                graphics.DrawString(node.Name, nameFont, nameBrush, new PointF(x + internalPadding + logoWidth, y + internalPadding));
                graphics.FillRectangle(typeBGBrush, x, y + internalPadding + nameFont.Height, width + internalPadding * 2, typeFont.Height + internalPadding);
                graphics.DrawString(node.Type, typeFont, typeBrush, new PointF(x + internalPadding, y + internalPadding + nameFont.Height + internalPadding / 2));
                graphics.DrawRectangle(boxPen, result[0]);

                node.Rect = node.RectEx = result[0];
            }

            if (symbolResult.Width > 0)
            {
                result[1] = symbolResult = DrawCount(x - symbolResult.Width, result[0].Top + (int)(result[0].Height / 2) - (int)(symbolResult.Height / 2), node.MinOccurs, node.MaxOccurs, measure);

                result[0] = new Rectangle(symbolResult.Left, Math.Min(result[0].Top, symbolResult.Top), result[0].Width + symbolResult.Width, Math.Max(result[0].Height, symbolResult.Height));
            }

            return result;
        }

        Rectangle[] DrawElement(int x, int y, Element element, bool measure, int alignedX)
        {
            Rectangle[] childrenResult = null;
            Rectangle[] attributesResult = null;

            if (element.Attributes != null && element.Attributes.Length > 0)
            {
                attributesResult = DrawAttributes(x, y, element.Attributes, true);
            }

            Rectangle[] nodeResult = DrawNode(x, y, element, true);

			if (nodeResult[0].Width > 0)
			{
				if (alignedX == 0)
				{
					alignedX = nodeResult[0].Right;
				}
			}
			else
			{
				alignedX = x;
			}

            if (element.Children != null && element.Children.Length > 0)
            {
                childrenResult = DrawElements(alignedX/*nodeResult[0].Right*/ + childrenSpacing, y, element.Children, measure);

                y = childrenResult[0].Top + (int)(childrenResult[0].Height / 2) - (int)(nodeResult[0].Height / 2);

				
				if (element.ContentModel == ContentModel.CHOICE && element.Name == "")
				{
					y = childrenResult[1].Top + (int)((childrenResult[childrenResult.Length - 1].Bottom - childrenResult[1].Top) / 2) - (int)(nodeResult[0].Height / 2);
				}
            }

            nodeResult = DrawNode(x, y, element, measure);

            Rectangle[] result = new Rectangle[2];

            result[0] = result[1] = nodeResult[0];

            if (childrenResult != null)
            {
                result[0] = new Rectangle(nodeResult[0].Left, childrenResult[0].Top, childrenResult[0].Right - nodeResult[0].Left, childrenResult[0].Height);

                if (!measure)
                {
                    Rectangle topElementResult = childrenResult[1];
                    Rectangle bottomElementResult = childrenResult[childrenResult.Length - 1];

                    if (childrenResult.Length > 2)
                    {
                        DrawSpanConnector(nodeResult[0].Right, nodeResult[0].Top + (int)(nodeResult[0].Height / 2) + 0, topElementResult.Left, topElementResult.Top + (int)(topElementResult.Height / 2), bottomElementResult.Left, bottomElementResult.Top + (int)(bottomElementResult.Height / 2), (float).5, element.ContentModel);
                    }
                    else
                    {
                        DrawConnector(nodeResult[0].Right, nodeResult[0].Top + (int)(nodeResult[0].Height / 2) + 0, topElementResult.Left, topElementResult.Top + (int)(topElementResult.Height / 2), (float).5);
                    }
                }
            }

            if (attributesResult != null)
            {
                int connectorOffset = 3;

                int ay = result[0].Top - attributesResult[0].Height - elementSpacing;

                if (childrenResult == null)
                {
                    ay = result[0].Top + (int)(result[0].Height / 2) - (int)(attributesResult[0].Height / 2) + 10;
                    connectorOffset = 0;
                }

                attributesResult = DrawAttributes(alignedX/*nodeResult[0].Right*/ + childrenSpacing, ay, element.Attributes, measure);

                if (!measure)
                {
                    DrawConnector(nodeResult[0].Right, (int)(nodeResult[0].Top + nodeResult[0].Height / 2) - connectorOffset, attributesResult[0].Left, attributesResult[0].Top + (int)(attributesResult[0].Height / 2), (float).25);
                }

                result[0] = new Rectangle(result[0].Left, attributesResult[0].Top, Math.Max(result[0].Right, attributesResult[0].Right) - result[0].Left, Math.Max(result[0].Bottom, attributesResult[0].Bottom) - attributesResult[0].Top);
            }

            //System.Console.WriteLine(element.Name + ": " + result[0].ToString());

            //if (!measure) System.Console.WriteLine("\t" + element.Name + ": " + result[0].ToString());

            return result;
        }

        Rectangle[] DrawAttributes(int x, int y, Attribute[] attributes, bool measure)
        {
            Rectangle[] result = DrawAttributes2(x, y, attributes, true);

            if (!measure)
            {
                graphics.FillRectangle(attributesBGBrush, result[0]);
                graphics.DrawRectangle(attributesPen, result[0]);

                DrawAttributes2(x, y, attributes, measure);
            }

            return result;
        }

        Rectangle[] DrawAttributes2(int x, int y, Attribute[] attributes, bool measure)
        {
            Rectangle[] result = new Rectangle[attributes.Length + 1];

            int originalX = x;

            int height = 0;

            int i = 1;

            foreach (Attribute attribute in attributes)
            {
                Rectangle[] where = DrawNode(x, y, attribute, true);

                if (where[1].Width == 0)
                {
                    where = DrawNode(x, y, attribute, measure);
                }
                else
                {
                    where = DrawNode(x + where[1].Width, y, attribute, measure);
                }

                result[i++] = where[0];

                height = where[0].Height;

                x = where[0].Right + attributeSpacing;
            }

            result[0] = new Rectangle(originalX - attributesPadding, y - attributesPadding, result[result.Length - 1].Right - originalX + attributesPadding * 2, height + attributesPadding * 2);

            return result;
        }

        Rectangle[] DrawElements(int x, int y, Element[] elements, bool measure)
        {
            Rectangle[] result = new Rectangle[elements.Length + 1];

            int originalY = y;
            int lowestY = y;

            int width = 0;
            int bottom = 0;

            int i = 1;

            Rectangle previous = new Rectangle();

            int alignedX = Int32.MinValue;

            foreach (Element element in elements)
            {
                Rectangle[] temp = DrawNode(x, y, element, true);
                if (temp[0].Right > alignedX) alignedX = temp[0].Right;
            }

            foreach (Element element in elements)
            {
                Rectangle[] temp = DrawElement(x, y, element, true, alignedX);

                if (i > 1 && temp[0].Top < previous.Bottom + elementSpacing)
                {
                    y += previous.Bottom + elementSpacing - temp[0].Top;
                }

                Rectangle[] elementResult = DrawElement(x, y, element, measure, alignedX);

                if (elementResult[0].Top < lowestY) lowestY = elementResult[0].Top;

                Rectangle where = elementResult[1];
                Rectangle total = elementResult[0];

                result[i++] = where;

                if (total.Width > width) width = total.Width;

                bottom = total.Bottom;

                y = bottom + elementSpacing;

                previous = total;
            }

            result[0] = new Rectangle(x, lowestY/*originalY*/, width, bottom - lowestY/*originalY*/);

            return result;
        }

        void DrawConnector(int x1, int y1, int x2, int y2, float turnPercentage)
        {
            int turn1 = (int)((x2 - x1) * turnPercentage);
            DrawConnector2(x1, y1, x2, y2, turn1);
        }

        void DrawConnector2(int x1, int y1, int x2, int y2, int turnDistance)
        {
            Point[] points = new Point[4] { new Point(x1, y1), new Point(x1 + turnDistance, y1), new Point(x1 + turnDistance, y2), new Point(x2, y2) };
            byte[] types = new byte[4] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line };

            GraphicsPath path = new GraphicsPath(points, types);

            graphics.DrawPath(linePen, path);
        }

        void DrawSpanConnector(int x1, int y1, int x2, int y2, int x3, int y3, float turnPercentage, ContentModel model)
        {
            int turn = (int)Math.Max((x2 - x1) * turnPercentage, (x3 - x1) * turnPercentage);

            if (model == ContentModel.ALL || model == ContentModel.CHOICE || model == ContentModel.MIXED)
            {
                Point[] points = new Point[4] { new Point(x1, y1), new Point(x1 + turn, y1), new Point(x2 - 5, y2), new Point(x2, y2) };
                byte[] types = new byte[4] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)((model == ContentModel.ALL) ? PathPointType.Bezier : PathPointType.Line), (byte)PathPointType.Line };

                GraphicsPath path = new GraphicsPath(points, types);
                graphics.DrawPath(linePen, path);

                points[2] = new Point(x3 - 5, y3);
                points[3] = new Point(x3, y3);

                path = new GraphicsPath(points, types);
                graphics.DrawPath(linePen, path);
            }
            else
            {
                DrawConnector2(x1, y1, x2, y2, turn);
                DrawConnector2(x1, y1, x3, y3, turn);
            }
        }

        Rectangle[] Draw(int x, int y, Node node, bool measure)
        {
            Rectangle[] result;

            if (node is Attribute)
            {
                result = DrawNode(x, y, node, measure);
            }
            else
            {
                result = DrawElement(x, y, node as Element, measure, 0);
            }

            return result;
        }

        string GetRelationshipSymbol(string count)
        {
            //if (count == "1") return "";

            return count;
        }

        Rectangle DrawRelationship(int x, int y, string relationship, string count, bool measure)
        {
            int c1x = x + 5;

            string[] parts = count.Split(':');

            string symbol1 = GetRelationshipSymbol(parts[0]);
            string symbol2 = GetRelationshipSymbol(parts[1]);

            Rectangle c1 = DrawCount(c1x, y, symbol1, true);

            int tx = c1.Right + 5;

            SizeF text = graphics.MeasureString(relationship, relationshipFont, new SizeF(relationshipWidth, relationshipHeight), centered);

            int x2 = tx + relationshipWidth;// (int)text.Width;
            int c2x = x2 + 5;

            Rectangle c2 = DrawCount(c2x, y, symbol2, true);

            int fx = c2.Right + 5;

            int height = Math.Max(Math.Max(c1.Height, c2.Height), (int)text.Height);

            if (!measure)
            {
                graphics.FillRectangle(bgBrush, tx + relationshipWidth / 2 - (int)text.Width / 2, y - (int)text.Height / 2, text.Width, text.Height);

                graphics.DrawLine(linePen, x, y, x + 5, y);
                DrawCount(c1x, y - c1.Height / 2, symbol1, measure);
                graphics.DrawLine(linePen, c1.Right, y, c1.Right + 5, y);
                graphics.DrawString(relationship, relationshipFont, relationshipBrush, new Rectangle(tx, y - relationshipHeight / 2, relationshipWidth, relationshipHeight), centered);
                graphics.DrawLine(linePen, x2, y, x2 + 5, y);
                DrawCount(c2x, y - c2.Height / 2, symbol2, measure);
                graphics.DrawLine(linePen, c2.Right, y, fx, y);
            }

            return new Rectangle(x, y, fx - x, height);
        }

        Rectangle[] DrawRelationships(int x, int y, Element element, bool measure)
        {
            Rectangle[] childrenResult = null;

            Rectangle[] nodeResult = DrawNode(x, y, element, true);

            int alignedX = nodeResult[0].Right;

            int shiftRight = 0;

            if (element.Children != null && element.Children.Length > 0)
            {
                foreach (Draw.Element child in element.Children)
                {
                    if (child.properties.ContainsKey("relationship"))
                    {
                        Rectangle relRect = DrawRelationship(0, 0, child.properties["relationship"], child.properties["count"], true);
                        if (relRect.Right > shiftRight) shiftRight = relRect.Right;
                    }
                }

                childrenResult = DrawElements(alignedX + shiftRight/*nodeResult[0].Right*/ + childrenSpacing, y, element.Children, measure);

                if (shiftRight > 0)
                {
                    int i = 1;

                    foreach (Draw.Element child in element.Children)
                    {
                        if (child.properties.ContainsKey("relationship"))
                        {
                            Rectangle relRect = DrawRelationship(alignedX + childrenSpacing, childrenResult[i].Top + childrenResult[i].Height / 2, child.properties["relationship"], child.properties["count"], measure);
                        }

                        ++i;
                    }
                }

                y = childrenResult[0].Top + (int)(childrenResult[0].Height / 2) - (int)(nodeResult[0].Height / 2);
            }

            if (!measure)
            {
                Rectangle temp = new Rectangle(nodeResult[0].Left - attributesPadding, y - attributesPadding, nodeResult[0].Width + attributesPadding * 2, nodeResult[0].Height + attributesPadding * 2);

                graphics.FillRectangle(attributesBGBrush, temp);
                graphics.DrawRectangle(attributesPen, temp);
            }

            nodeResult = DrawNode(x, y, element, measure);

            Rectangle[] result = new Rectangle[2];

            result[0] = result[1] = nodeResult[0];

            if (childrenResult != null)
            {
                result[0] = new Rectangle(nodeResult[0].Left, childrenResult[0].Top, childrenResult[0].Right - nodeResult[0].Left, childrenResult[0].Height);

                if (!measure)
                {
                    int yInc = nodeResult[0].Height / (childrenResult.Length - 1);

                    int x1 = nodeResult[0].Right;
                    int y1 = nodeResult[0].Top + (nodeResult[0].Height / 2) - (yInc * (childrenResult.Length - 2)) / 2;

                    for (int i = 1; i < childrenResult.Length; ++i)
                    {
                        Rectangle dst = childrenResult[i];

                        int x2 = dst.Left;
                        int y2 = dst.Top + (dst.Height / 2);

                       // Point[] points = new Point[4] { new Point(x1, y1), new Point(x1 + 5, y1), new Point(x2 - 5, y2), new Point(x2, y2) };
                        Point[] points = new Point[4] { new Point(x1, y1), new Point(x1 + 5, y1), new Point(x2 - 5 - shiftRight, y2), new Point(x2, y2) };
                        byte[] types = new byte[4] { (byte)PathPointType.Start, (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line };

                        GraphicsPath path = new GraphicsPath(points, types);

                        graphics.DrawPath(linePen, path);

                        y1 += yInc;
                    }

                    if (shiftRight > 0)
                    {
                        int i = 1;

                        foreach (Draw.Element child in element.Children)
                        {
                            if (child.properties.ContainsKey("relationship"))
                            {
                                Rectangle relRect = DrawRelationship(alignedX + childrenSpacing, childrenResult[i].Top + childrenResult[i].Height / 2, child.properties["relationship"], child.properties["count"], measure);
                            }

                            ++i;
                        }
                    }
                }
            }

            return result;
        }

        public void DrawRelationships(Element node, string fileName)
        {
            Initialize();

            Rectangle[] bounds = DrawRelationships(0, 0, node, true);

            //System.Console.WriteLine(node.Name + ": " + bounds[0].ToString());

            Resize(bounds[0].Width + 10, bounds[0].Height + 10);

            int x = 5;
            int y = 5;

            if (bounds[0].Top < 0)
            {
                y += -bounds[0].Top;
            }

            bounds = DrawRelationships(x, y, node, false);

            bitmap.Save(fileName);

            Map(node, fileName.Replace(@"\diagrams\", @"\temp\") + ".html");
        }

        public void Draw(Node node, string fileName)
        {
            Initialize();

            Rectangle[] bounds = Draw(0, 0, node, true);

            //System.Console.WriteLine(node.Name + ": " + bounds[0].ToString());

            Resize(bounds[0].Width + 10, bounds[0].Height + 10);

            int x = 5;
            int y = 5;

            if (bounds[0].Top < 0)
            {
                y += -bounds[0].Top;
            }

            bounds = Draw(x, y, node, false);

            bitmap.Save(fileName);

            Map(node, fileName.Replace(@"\diagrams\", @"\temp\") + ".html");
        }

        public void Map(Node node, string fileName)
        {
            System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(new System.IO.StreamWriter(fileName));

            writer.Formatting = System.Xml.Formatting.Indented;
            /*
            writer.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);
            writer.WriteStartElement("html");
            writer.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
            writer.WriteStartElement("head");
            writer.WriteStartElement("meta");
            writer.WriteAttributeString("http-equiv", "Content-Type");
            writer.WriteAttributeString("content", "text/html;charset=utf-8");
            writer.WriteEndElement();
            writer.WriteElementString("title", node.Name);
            writer.WriteStartElement("style");
            writer.WriteAttributeString("type", "text/css");
            writer.WriteString("a img { border: 0 } img { border : 0 }");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("body");

            writer.WriteStartElement("img");
            writer.WriteAttributeString("alt", node.Name);
            writer.WriteAttributeString("src", node.Name + ".png");
            writer.WriteAttributeString("usemap", "#map");
            writer.WriteEndElement();
            */
            writer.WriteStartElement("map");
            writer.WriteAttributeString("name", node.Name + "__" + "map");
			writer.WriteAttributeString("id", node.Name + "__" + "map");

            Map(node, writer, "");

            writer.WriteEndElement();
            /*
            writer.WriteEndElement();
            */
            writer.Close();
        }

        void Map(Node node, System.Xml.XmlTextWriter writer, string parent)
        {
            writer.WriteStartElement("area");
            writer.WriteAttributeString("alt", node.Name);
            //writer.WriteAttributeString("href", "javascript:window.alert('" + node.Name + "')");

            string href = parent;

            if (href == "")
            {
                href = "#";
            }

            if (parent != "")
            {
                href += "__";

                if (node is Attribute)
                {
                    href += "_";
                }
            }

            href += node.Name;
            
            writer.WriteAttributeString("href", href);
            
            writer.WriteAttributeString("shape", "rect");
            writer.WriteAttributeString("coords", node.Rect.Left.ToString() + "," + node.Rect.Top.ToString() + "," + node.Rect.Right.ToString() + "," + node.Rect.Bottom.ToString());
            writer.WriteEndElement();

            if (node is Element)
            {
                Element element = node as Element;

                if (element.Attributes != null && element.Attributes.Length > 0)
                {
                    foreach (Attribute attribute in element.Attributes)
                    {
                        Map(attribute, writer, href);
                    }
                }

                if (element.Children != null && element.Children.Length > 0)
                {
                    foreach (Element child in element.Children)
                    {
                        Map(child, writer, href);
                    }
                }
            }
        }
    }
}
