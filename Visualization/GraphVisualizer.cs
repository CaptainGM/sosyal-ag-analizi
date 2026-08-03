using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SocialNetworkAnalysis.Models;

namespace SocialNetworkAnalysis.Visualization
{
    public class GraphVisualizer
    {
        private Canvas canvas;
        private ArticleGraph graph;
        private Dictionary<string, Point> nodePositions;
        private Dictionary<string, Ellipse> nodeEllipses;
        private HashSet<string> highlightedNodes;
        private HashSet<string> hCoreNodes;
        private HashSet<string> newlyAddedNodes;
        private HashSet<string> clickedNodes;
        private List<(string from, string to)> highlightedEdges;
        private Dictionary<string, int> kCoreValues;
        private Border tooltipBorder;
        private TextBlock tooltipText;

        public event Action<string> OnNodeClicked;
        public event Action<string> OnNodeHover;

        private static readonly SolidColorBrush NormalFill = new SolidColorBrush(Color.FromRgb(255, 243, 176));
        private static readonly SolidColorBrush HCoreFill = new SolidColorBrush(Color.FromRgb(255, 180, 180));
        private static readonly SolidColorBrush HighlightFill = new SolidColorBrush(Color.FromRgb(180, 255, 180));
        private static readonly SolidColorBrush NewNodeFill = new SolidColorBrush(Color.FromRgb(180, 200, 255));
        private static readonly SolidColorBrush ClickedNodeFill = new SolidColorBrush(Color.FromRgb(255, 150, 150));
        private static readonly SolidColorBrush KCore1Fill = new SolidColorBrush(Color.FromRgb(255, 243, 176));
        private static readonly SolidColorBrush KCore2Fill = new SolidColorBrush(Color.FromRgb(200, 230, 255));
        private static readonly SolidColorBrush KCore3Fill = new SolidColorBrush(Color.FromRgb(200, 255, 200));
        private static readonly SolidColorBrush KCoreEdgeBrush = new SolidColorBrush(Color.FromRgb(0, 100, 255));

        private const double NodeRadius = 30;

        public GraphVisualizer(Canvas canvas)
        {
            this.canvas = canvas;
            nodePositions = new Dictionary<string, Point>();
            nodeEllipses = new Dictionary<string, Ellipse>();
            highlightedNodes = new HashSet<string>();
            hCoreNodes = new HashSet<string>();
            newlyAddedNodes = new HashSet<string>();
            clickedNodes = new HashSet<string>();
            highlightedEdges = new List<(string, string)>();
            kCoreValues = new Dictionary<string, int>();
            CreateTooltip();
        }

        private void CreateTooltip()
        {
            tooltipBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 120)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Visibility = Visibility.Collapsed
            };
            tooltipText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 11
            };
            tooltipBorder.Child = tooltipText;
            Canvas.SetZIndex(tooltipBorder, 1000);
        }

        public void DrawGraph(ArticleGraph graph, bool showIdOrderEdges = true)
        {
            this.graph = graph;
            canvas.Children.Clear();
            nodePositions.Clear();
            nodeEllipses.Clear();

            var articles = graph.GetAllArticles();
            if (articles.Count == 0) return;

            CalculateForceDirectedLayout(articles);

            DrawReferenceEdges();
            if (showIdOrderEdges) DrawIdOrderEdges();
            DrawHighlightedEdges();
            DrawNodes(articles);
            canvas.Children.Add(tooltipBorder);
        }

        public void SetHCoreNodes(List<Article> hCoreArticles)
        {
            hCoreNodes = new HashSet<string>(hCoreArticles.Select(a => a.Id));
        }

        public void SetNewlyAddedNodes(List<string> nodeIds)
        {
            newlyAddedNodes = new HashSet<string>(nodeIds);
        }

        public void SetClickedNodes(List<string> nodeIds)
        {
            clickedNodes = new HashSet<string>(nodeIds);
        }

        public void SetKCoreValues(Dictionary<string, int> values)
        {
            kCoreValues = values ?? new Dictionary<string, int>();
        }

        public void HighlightNodes(List<string> nodeIds)
        {
            highlightedNodes = new HashSet<string>(nodeIds);
        }

        public void HighlightPath(List<string> path)
        {
            highlightedEdges.Clear();
            highlightedNodes = new HashSet<string>(path);
            for (int i = 0; i < path.Count - 1; i++)
                highlightedEdges.Add((path[i], path[i + 1]));
        }

        public void ClearHighlights()
        {
            highlightedNodes.Clear();
            highlightedEdges.Clear();
            hCoreNodes.Clear();
            newlyAddedNodes.Clear();
            clickedNodes.Clear();
            kCoreValues.Clear();
        }

        public void HighlightNode(string nodeId)
        {
            // Sadece seçili düğümü vurgula - diğer vurguları temizle
            highlightedNodes.Clear();
            highlightedNodes.Add(nodeId);

            // Düğümün rengini güncelle
            if (nodeEllipses.TryGetValue(nodeId, out var ellipse))
            {
                ellipse.Fill = new SolidColorBrush(Color.FromRgb(255, 200, 100)); // Turuncu vurgu
                ellipse.Stroke = new SolidColorBrush(Color.FromRgb(255, 100, 0));
                ellipse.StrokeThickness = 4;
            }
        }

        public string GetKCoreValue(string nodeId)
        {
            if (kCoreValues.TryGetValue(nodeId, out int value))
                return value.ToString();
            return "N/A";
        }

        private void CalculateForceDirectedLayout(List<Article> articles)
        {
            double w = canvas.ActualWidth > 0 ? canvas.ActualWidth : 700;
            double h = canvas.ActualHeight > 0 ? canvas.ActualHeight : 500;
            double cx = w / 2, cy = h / 2;
            int n = articles.Count;

            var positions = new Dictionary<string, Point>();
            var velocities = new Dictionary<string, Point>();
            var rand = new Random(42);

            foreach (var a in articles)
            {
                double angle = rand.NextDouble() * 2 * Math.PI;
                double r = rand.NextDouble() * Math.Min(w, h) * 0.3;
                positions[a.Id] = new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
                velocities[a.Id] = new Point(0, 0);
            }

            double k = Math.Sqrt(w * h / Math.Max(n, 1)) * 0.8;
            double cooling = 0.95;
            double temp = w / 4;

            for (int iter = 0; iter < 80; iter++)
            {
                var forces = articles.ToDictionary(a => a.Id, _ => new Point(0, 0));

                for (int i = 0; i < articles.Count; i++)
                {
                    for (int j = i + 1; j < articles.Count; j++)
                    {
                        var a1 = articles[i].Id;
                        var a2 = articles[j].Id;
                        double dx = positions[a2].X - positions[a1].X;
                        double dy = positions[a2].Y - positions[a1].Y;
                        double dist = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1);
                        double repulsion = k * k / dist * 0.5;
                        double fx = dx / dist * repulsion;
                        double fy = dy / dist * repulsion;
                        forces[a1] = new Point(forces[a1].X - fx, forces[a1].Y - fy);
                        forces[a2] = new Point(forces[a2].X + fx, forces[a2].Y + fy);
                    }
                }

                foreach (var edge in graph.GetAllReferenceEdges())
                {
                    if (!positions.ContainsKey(edge.from) || !positions.ContainsKey(edge.to)) continue;
                    double dx = positions[edge.to].X - positions[edge.from].X;
                    double dy = positions[edge.to].Y - positions[edge.from].Y;
                    double dist = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1);
                    double attraction = dist * dist / k * 0.1;
                    double fx = dx / dist * attraction;
                    double fy = dy / dist * attraction;
                    forces[edge.from] = new Point(forces[edge.from].X + fx, forces[edge.from].Y + fy);
                    forces[edge.to] = new Point(forces[edge.to].X - fx, forces[edge.to].Y - fy);
                }

                foreach (var a in articles)
                {
                    var f = forces[a.Id];
                    double mag = Math.Sqrt(f.X * f.X + f.Y * f.Y);
                    if (mag > 0)
                    {
                        double limitedMag = Math.Min(mag, temp);
                        double newX = positions[a.Id].X + f.X / mag * limitedMag;
                        double newY = positions[a.Id].Y + f.Y / mag * limitedMag;
                        newX = Math.Max(NodeRadius + 10, Math.Min(w - NodeRadius - 10, newX));
                        newY = Math.Max(NodeRadius + 10, Math.Min(h - NodeRadius - 10, newY));
                        positions[a.Id] = new Point(newX, newY);
                    }
                }
                temp *= cooling;
            }

            nodePositions = positions;
        }

        private void DrawNodes(List<Article> articles)
        {
            foreach (var article in articles)
            {
                if (!nodePositions.TryGetValue(article.Id, out Point pos)) continue;

                Brush fill = NormalFill;
                if (clickedNodes.Contains(article.Id))
                    fill = ClickedNodeFill;
                else if (newlyAddedNodes.Contains(article.Id))
                    fill = NewNodeFill;
                else if (highlightedNodes.Contains(article.Id))
                    fill = HighlightFill;
                else if (hCoreNodes.Contains(article.Id))
                    fill = HCoreFill;
                else if (kCoreValues.TryGetValue(article.Id, out int kVal))
                {
                    if (kVal >= 3) fill = KCore3Fill;
                    else if (kVal >= 2) fill = KCore2Fill;
                }

                var ellipse = new Ellipse
                {
                    Width = NodeRadius * 2,
                    Height = NodeRadius * 2,
                    Fill = fill,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.5,
                    Cursor = Cursors.Hand,
                    Tag = article.Id
                };

                ellipse.MouseEnter += Ellipse_MouseEnter;
                ellipse.MouseLeave += Ellipse_MouseLeave;
                ellipse.MouseLeftButtonUp += Ellipse_MouseLeftButtonUp;

                Canvas.SetLeft(ellipse, pos.X - NodeRadius);
                Canvas.SetTop(ellipse, pos.Y - NodeRadius);
                canvas.Children.Add(ellipse);
                nodeEllipses[article.Id] = ellipse;

                int citationCount = graph.GetCitations(article.Id).Count;
                var citation = new TextBlock
                {
                    Text = citationCount.ToString(),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    IsHitTestVisible = false
                };
                citation.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(citation, pos.X - citation.DesiredSize.Width / 2);
                Canvas.SetTop(citation, pos.Y - NodeRadius + 4);
                canvas.Children.Add(citation);

                string shortId = article.ShortId;
                if (shortId.Length > 8) shortId = shortId.Substring(shortId.Length - 8);
                var idText = new TextBlock
                {
                    Text = shortId,
                    FontSize = 7,
                    Foreground = Brushes.DarkGray,
                    IsHitTestVisible = false
                };
                idText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(idText, pos.X - idText.DesiredSize.Width / 2);
                Canvas.SetTop(idText, pos.Y - 6);
                canvas.Children.Add(idText);

                var author = new TextBlock
                {
                    Text = article.AuthorAbbreviation,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    IsHitTestVisible = false
                };
                author.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(author, pos.X - author.DesiredSize.Width / 2);
                Canvas.SetTop(author, pos.Y + 6);
                canvas.Children.Add(author);
            }
        }

        private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is string articleId)
            {
                var article = graph.GetArticle(articleId);
                if (article != null)
                {
                    int citationCount = graph.GetCitations(articleId).Count;
                    int referenceCount = graph.GetReferences(articleId).Count;
                    string authors = string.Join(", ", article.Authors.Take(2));
                    if (article.Authors.Count > 2) authors += "...";

                    // k-core değeri varsa göster
                    string kCoreInfo = kCoreValues.ContainsKey(articleId) ? $"\nk-core: {kCoreValues[articleId]}" : "";

                    tooltipText.Text = $"ID: {article.ShortId}\n" +
                                       $"Yazar: {authors}\n" +
                                       $"Başlık: {(article.Title.Length > 45 ? article.Title.Substring(0, 45) + "..." : article.Title)}\n" +
                                       $"Yıl: {article.Year}\n" +
                                       $"Atıf Sayısı: {citationCount}\n" +
                                       $"Referans Sayısı: {referenceCount}" +
                                       kCoreInfo;

                    // Dinamik konumlandırma - node'un ters köşesine tooltip yerleştir
                    var pos = e.GetPosition(canvas);
                    double tooltipWidth = 300;
                    double tooltipHeight = 120;
                    double canvasWidth = canvas.ActualWidth;
                    double canvasHeight = canvas.ActualHeight;

                    double left, top;

                    // Eğer node sağ tarafta ise tooltip sola, sol tarafta ise sağa
                    if (pos.X > canvasWidth / 2)
                        left = pos.X - tooltipWidth - 10;
                    else
                        left = pos.X + 30;

                    // Eğer node üst tarafta ise tooltip alta, alt tarafta ise üste
                    if (pos.Y > canvasHeight / 2)
                        top = pos.Y - tooltipHeight - 10;
                    else
                        top = pos.Y + 30;

                    // Canvas sınırlarını kontrol et
                    left = Math.Max(5, Math.Min(left, canvasWidth - tooltipWidth - 5));
                    top = Math.Max(5, Math.Min(top, canvasHeight - tooltipHeight - 5));

                    Canvas.SetLeft(tooltipBorder, left);
                    Canvas.SetTop(tooltipBorder, top);
                    tooltipBorder.Visibility = Visibility.Visible;
                    OnNodeHover?.Invoke(articleId);
                }
            }
        }

        private void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            tooltipBorder.Visibility = Visibility.Collapsed;
        }

        private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is string articleId)
            {
                OnNodeClicked?.Invoke(articleId);
            }
        }

        public void DrawKCoreHighlight(ArticleGraph kCoreGraph, int k)
        {
            var kCoreIds = new HashSet<string>(kCoreGraph.GetAllArticles().Select(a => a.Id));
            foreach (var edge in kCoreGraph.GetUndirectedEdges())
            {
                if (!nodePositions.TryGetValue(edge.from, out Point p1) || !nodePositions.TryGetValue(edge.to, out Point p2))
                    continue;
                DrawLine(p1, p2, KCoreEdgeBrush, 3);
            }
            foreach (var id in kCoreIds)
            {
                if (nodeEllipses.TryGetValue(id, out Ellipse ellipse))
                {
                    ellipse.Stroke = KCoreEdgeBrush;
                    ellipse.StrokeThickness = 3;
                }
            }
        }

        private void DrawLine(Point start, Point end, Brush color, double thickness)
        {
            canvas.Children.Add(new Line { X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y, Stroke = color, StrokeThickness = thickness });
        }

        private void DrawReferenceEdges()
        {
            foreach (var (from, to) in graph.GetAllReferenceEdges())
            {
                if (!nodePositions.TryGetValue(from, out Point p1) || !nodePositions.TryGetValue(to, out Point p2))
                    continue;
                DrawArrow(p1, p2, Brushes.Black, 1.2);
            }
        }

        private void DrawIdOrderEdges()
        {
            foreach (var (from, to) in graph.GetIdOrderEdges())
            {
                if (!nodePositions.TryGetValue(from, out Point p1) || !nodePositions.TryGetValue(to, out Point p2))
                    continue;
                DrawCurvedArrow(p1, p2, Brushes.Green, 1.0);
            }
        }

        private void DrawHighlightedEdges()
        {
            foreach (var (from, to) in highlightedEdges)
            {
                if (!nodePositions.TryGetValue(from, out Point p1) || !nodePositions.TryGetValue(to, out Point p2))
                    continue;
                DrawArrow(p1, p2, Brushes.Red, 3);
            }
        }

        private void DrawArrow(Point start, Point end, Brush color, double thickness)
        {
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < NodeRadius * 2 + 5) return;

            double nx = dx / len, ny = dy / len;
            var p1 = new Point(start.X + nx * NodeRadius, start.Y + ny * NodeRadius);
            var p2 = new Point(end.X - nx * (NodeRadius + 8), end.Y - ny * (NodeRadius + 8));

            canvas.Children.Add(new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = color, StrokeThickness = thickness });

            double angle = 0.4;
            double arrowLen = 8;
            var tip = new Point(end.X - nx * NodeRadius, end.Y - ny * NodeRadius);
            var left = new Point(tip.X - arrowLen * (nx * Math.Cos(angle) + ny * Math.Sin(angle)),
                                 tip.Y - arrowLen * (ny * Math.Cos(angle) - nx * Math.Sin(angle)));
            var right = new Point(tip.X - arrowLen * (nx * Math.Cos(angle) - ny * Math.Sin(angle)),
                                  tip.Y - arrowLen * (ny * Math.Cos(angle) + nx * Math.Sin(angle)));
            canvas.Children.Add(new Polygon { Points = new PointCollection { tip, left, right }, Fill = color });
        }

        private void DrawCurvedArrow(Point start, Point end, Brush color, double thickness)
        {
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < NodeRadius * 2 + 5) return;

            double nx = dx / len, ny = dy / len;
            var p1 = new Point(start.X + nx * NodeRadius, start.Y + ny * NodeRadius);
            var p2 = new Point(end.X - nx * (NodeRadius + 8), end.Y - ny * (NodeRadius + 8));

            double offset = len * 0.15;
            var ctrl = new Point((p1.X + p2.X) / 2 - ny * offset, (p1.Y + p2.Y) / 2 + nx * offset);

            var geo = new PathGeometry();
            var fig = new PathFigure { StartPoint = p1 };
            fig.Segments.Add(new QuadraticBezierSegment(ctrl, p2, true));
            geo.Figures.Add(fig);
            canvas.Children.Add(new Path { Data = geo, Stroke = color, StrokeThickness = thickness });

            var tip = new Point(end.X - nx * NodeRadius, end.Y - ny * NodeRadius);
            double cdx = tip.X - ctrl.X, cdy = tip.Y - ctrl.Y;
            double clen = Math.Sqrt(cdx * cdx + cdy * cdy);
            if (clen > 0)
            {
                double cnx = cdx / clen, cny = cdy / clen;
                double angle = 0.4, arrowLen = 7;
                var left = new Point(tip.X - arrowLen * (cnx * Math.Cos(angle) + cny * Math.Sin(angle)),
                                     tip.Y - arrowLen * (cny * Math.Cos(angle) - cnx * Math.Sin(angle)));
                var right = new Point(tip.X - arrowLen * (cnx * Math.Cos(angle) - cny * Math.Sin(angle)),
                                      tip.Y - arrowLen * (cny * Math.Cos(angle) + cnx * Math.Sin(angle)));
                canvas.Children.Add(new Polygon { Points = new PointCollection { tip, left, right }, Fill = color });
            }
        }
    }
}
