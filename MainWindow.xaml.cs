using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SocialNetworkAnalysis.Algorithms;
using SocialNetworkAnalysis.Data;
using SocialNetworkAnalysis.Models;
using SocialNetworkAnalysis.Visualization;

namespace SocialNetworkAnalysis
{
    public partial class MainWindow : Window
    {
        private ArticleGraph graph;
        private ArticleGraph currentDisplayGraph;
        private GraphVisualizer visualizer;
        private List<Article> allArticles;
        private BFS bfsAlgorithm;
        private DFS dfsAlgorithm;
        private HCore hCoreAlgorithm;
        private KCore kCoreAlgorithm;
        private BetweennessCentrality betweennessAlgorithm;
        private HashSet<string> displayedNodeIds;
        private List<string> clickedNodeHistory;
        private string selectedArticleId;
        private List<List<string>> lastComponents;
        private List<List<string>> lastCycles;
        private int componentsShowCount;
        private int cyclesShowCount;
        private string lastResultType; // "components" veya "cycles"

        public MainWindow()
        {
            InitializeComponent();
            allArticles = new List<Article>();
            displayedNodeIds = new HashSet<string>();
            clickedNodeHistory = new List<string>();
            selectedArticleId = "";
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            visualizer = new GraphVisualizer(graphCanvas);
            visualizer.OnNodeClicked += OnGraphNodeClicked;
            txtStats.Text = "Veri yüklemek için 'Veri Yükle' butonuna tıklayın.";
            SetResults("Henüz analiz yapılmadı.");
        }

        private void SetResults(string text)
        {
            txtResults.Text = text;
            txtShowMore.Visibility = Visibility.Collapsed;
        }

        private void OnGraphNodeClicked(string articleId)
        {
            if (graph == null) return;

            var article = graph.GetArticle(articleId);
            if (article == null) return;

            // h-index hesapla (proje gereksinimi)
            var (hIndex, hCore, hMedian) = hCoreAlgorithm.CalculateArticleHIndex(articleId);

            // Seçili makale bilgilerini güncelle
            txtArticleSearch.Text = article.Title;
            txtSelectedArticleId.Text = article.ShortId;
            selectedArticleId = articleId;
            txtHCoreInfo.Text = $"h-index: {hIndex}, h-median: {hMedian:F1}";

            // Tıklanan düğümü kaydet
            clickedNodeHistory.Add(articleId);
            var newNodeIds = new List<string>();

            // h-core düğümlerini grafa ekle (proje gereksinimi - Şekil 4)
            foreach (var hCoreArticle in hCore)
            {
                if (!displayedNodeIds.Contains(hCoreArticle.Id))
                {
                    displayedNodeIds.Add(hCoreArticle.Id);
                    newNodeIds.Add(hCoreArticle.Id);
                }
            }

            if (!displayedNodeIds.Contains(articleId))
                displayedNodeIds.Add(articleId);

            // Alt grafı güncelle
            currentDisplayGraph = graph.CreateSubgraph(displayedNodeIds);

            // Renklendirme ayarları
            visualizer.ClearHighlights();
            visualizer.SetClickedNodes(clickedNodeHistory);
            visualizer.SetNewlyAddedNodes(newNodeIds);
            visualizer.SetHCoreNodes(hCore);
            visualizer.DrawGraph(currentDisplayGraph, chkShowIdEdges.IsChecked ?? true);

            UpdateDisplayStats();

            // Sonuçları göster
            int citationCount = graph.GetCitations(articleId).Count;
            int referenceCount = graph.GetReferences(articleId).Count;
            string kCoreValue = visualizer.GetKCoreValue(articleId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🎯 MAKALE DETAYLARI\n");
            sb.AppendLine($"{'=' * 50}\n");
            sb.AppendLine($"📝 Temel Bilgiler:");
            sb.AppendLine($"ID: {article.ShortId}");
            sb.AppendLine($"Başlık: {article.Title}");
            sb.AppendLine($"Yıl: {article.Year}");
            sb.AppendLine($"Yazarlar: {string.Join(", ", article.Authors)}\n");

            sb.AppendLine($"📊 Ağ Metrikleri:");
            sb.AppendLine($"Atıf Sayısı: {citationCount}");
            sb.AppendLine($"Referans Sayısı: {referenceCount}");
            if (!string.IsNullOrEmpty(kCoreValue) && kCoreValue != "N/A")
                sb.AppendLine($"k-core Değeri: {kCoreValue}");
            sb.AppendLine($"\n📈 h-index Analizi:");
            sb.AppendLine($"h-index: {hIndex}");
            sb.AppendLine($"h-median: {hMedian:F1}");
            sb.AppendLine($"\n🔗 h-core Üyeleri ({hCore.Count} makale):");

            foreach (var hCoreArticle in hCore.Take(15))
            {
                var citCount = graph.GetCitations(hCoreArticle.Id).Count;
                var refCount = graph.GetReferences(hCoreArticle.Id).Count;
                sb.AppendLine($"  • {hCoreArticle.ShortId} - {citCount} atıf, {refCount} referans");
            }

            if (hCore.Count > 15)
                sb.AppendLine($"  ... ve {hCore.Count - 15} makale daha");

            if (newNodeIds.Count > 0)
                sb.AppendLine($"\n✨ Grafa {newNodeIds.Count} yeni düğüm eklendi!");

            txtResults.Text = sb.ToString();
            txtShowMore.Visibility = Visibility.Collapsed;
        }

        private void BtnLoadData_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON dosyaları (*.json)|*.json|Tüm dosyalar (*.*)|*.*",
                Title = "Makale Verisi Seç",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (Directory.Exists(dataPath))
            {
                dialog.InitialDirectory = dataPath;
            }

            if (dialog.ShowDialog() == true)
            {
                LoadData(dialog.FileName);
            }
        }

        private void LoadData(string filePath)
        {
            try
            {
                graph = DataLoader.LoadFromJson(filePath);
                allArticles = graph.GetAllArticles();

                bfsAlgorithm = new BFS(graph);
                dfsAlgorithm = new DFS(graph);
                hCoreAlgorithm = new HCore(graph);
                kCoreAlgorithm = new KCore(graph);
                betweennessAlgorithm = new BetweennessCentrality(graph);

                displayedNodeIds.Clear();
                clickedNodeHistory.Clear();

                UpdateArticleList(allArticles);
                UpdateComboBoxes();
                UpdateStats();

                DrawGraphSubset(200); // Başlangıçta sadece en çok atıf alan 200 makaleyi göster

                txtResults.Text = $"✅ {allArticles.Count} makale başarıyla yüklendi.\n\n" +
                                  $"{graph.GetDetailedStats()}\n\n" +
                                  $"İpucu: Grafa daha fazla makale eklemek için düğümlere tıklayın.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrawGraphSubset(int maxNodes)
        {
            if (graph == null) return;

            var topArticles = allArticles
                .OrderByDescending(a => a.CitationCount)
                .Take(maxNodes)
                .ToList();

            displayedNodeIds = new HashSet<string>(topArticles.Select(a => a.Id));
            currentDisplayGraph = graph.CreateSubgraph(displayedNodeIds);

            visualizer.ClearHighlights();
            visualizer.DrawGraph(currentDisplayGraph, chkShowIdEdges.IsChecked ?? true);
        }

        private void UpdateArticleList(List<Article> articles)
        {
            lstArticles.ItemsSource = articles.OrderByDescending(a => a.CitationCount).ToList();
        }

        private void UpdateComboBoxes()
        {
            PopulateComboBox(cmbStartArticle, "");
            PopulateComboBox(cmbEndArticle, "");
            if (cmbStartArticle.Items.Count > 0) cmbStartArticle.SelectedIndex = 0;
            if (cmbEndArticle.Items.Count > 1) cmbEndArticle.SelectedIndex = 1;
        }

        private void PopulateComboBox(ComboBox cmb, string searchText)
        {
            var filtered = allArticles
                .Where(a => string.IsNullOrEmpty(searchText) ||
                           a.Title.ToLower().Contains(searchText.ToLower()) ||
                           a.ShortId.ToLower().Contains(searchText.ToLower()))
                .OrderByDescending(a => a.CitationCount)
                .Select(a => new ComboBoxItem
                {
                    Content = $"{a.ShortId} - {a.Title}",
                    Tag = a.Id
                })
                .ToList();

            cmb.Items.Clear();
            foreach (var item in filtered)
            {
                cmb.Items.Add(item);
            }
        }

        private void TxtStartSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            PopulateComboBox(cmbStartArticle, txtStartSearch.Text);
            if (cmbStartArticle.Items.Count > 0) cmbStartArticle.SelectedIndex = 0;
        }

        private void TxtEndSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            PopulateComboBox(cmbEndArticle, txtEndSearch.Text);
            if (cmbEndArticle.Items.Count > 0) cmbEndArticle.SelectedIndex = 0;
        }

        private void UpdateStats()
        {
            if (graph == null) return;
            txtStats.Text = graph.GetDetailedStats();
        }

        private void UpdateDisplayStats()
        {
            if (currentDisplayGraph == null) return;
            txtStats.Text = $"Graf İstatistikleri:\n{currentDisplayGraph.GetDetailedStats()}";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allArticles == null || allArticles.Count == 0) return;

            string search = txtSearch.Text.ToLower();
            var filtered = allArticles
                .Where(a => a.Title.ToLower().Contains(search) ||
                           a.Authors.Any(auth => auth.ToLower().Contains(search)) ||
                           a.ShortId.Contains(search))
                .OrderByDescending(a => a.CitationCount)
                .ToList();

            lstArticles.ItemsSource = filtered;
        }

        private void LstArticles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstArticles.SelectedItem is Article article)
            {
                ShowArticleDetails(article);
                txtSelectedArticleId.Text = article.ShortId;
                selectedArticleId = article.Id;
            }
        }

        private void TxtArticleSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (allArticles == null || allArticles.Count == 0) return;

            string searchText = txtArticleSearch.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                lstArticleSearchResults.Visibility = Visibility.Collapsed;
                return;
            }

            var filtered = allArticles
                .Where(a => a.Title.ToLower().Contains(searchText) ||
                           a.ShortId.ToLower().Contains(searchText) ||
                           a.Authors.Any(auth => auth.ToLower().Contains(searchText)))
                .OrderByDescending(a => a.CitationCount)
                .ToList();

            lstArticleSearchResults.ItemsSource = filtered;
            lstArticleSearchResults.Visibility = filtered.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LstArticleSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstArticleSearchResults.SelectedItem is Article article)
            {
                txtArticleSearch.Text = article.Title;
                txtSelectedArticleId.Text = article.ShortId;
                selectedArticleId = article.Id;
                lstArticleSearchResults.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowArticleDetails(Article article)
        {
            string authors = string.Join(", ", article.Authors.Take(3));
            if (article.Authors.Count > 3) authors += $" ve {article.Authors.Count - 3} diğer";

            string keywords = string.Join(", ", article.Keywords.Take(5));
            if (article.Keywords.Count > 5) keywords += "...";

            int citationCount = graph.GetCitations(article.Id).Count;
            int referenceCount = graph.GetReferences(article.Id).Count;

            txtResults.Text = $"📄 {article.Title}\n\n" +
                             $"📅 Yıl: {article.Year}\n" +
                             $"👥 Yazarlar: {authors}\n" +
                             $"📊 Atıf Sayısı: {citationCount}\n" +
                             $"🔗 Referans Sayısı: {referenceCount}\n" +
                             $"📚 Dergi: {article.Venue}\n" +
                             $"🏷️ Anahtar Kelimeler: {keywords}\n" +
                             $"🆔 ID: {article.ShortId}";
        }

        private void BtnCalculateHIndex_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Önce veri yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string inputId = txtSelectedArticleId.Text.Trim();
            if (string.IsNullOrEmpty(inputId) && string.IsNullOrEmpty(selectedArticleId))
            {
                MessageBox.Show("Makale seçin veya arayın!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Article article = null;
            if (!string.IsNullOrEmpty(selectedArticleId))
            {
                article = graph.GetArticle(selectedArticleId);
            }
            if (article == null)
            {
                article = allArticles.FirstOrDefault(a => a.ShortId == inputId || a.Id.EndsWith(inputId) || a.Id == inputId);
            }

            if (article == null)
            {
                MessageBox.Show("Makale bulunamadı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (hIndex, hCore, hMedian) = hCoreAlgorithm.CalculateArticleHIndex(article.Id);

            txtHCoreInfo.Text = $"h-index: {hIndex}, h-median: {hMedian:F1}";

            clickedNodeHistory.Clear();
            clickedNodeHistory.Add(article.Id);
            displayedNodeIds = new HashSet<string>(hCore.Select(a => a.Id));
            displayedNodeIds.Add(article.Id);

            currentDisplayGraph = graph.CreateSubgraph(displayedNodeIds);

            visualizer.ClearHighlights();
            visualizer.SetClickedNodes(new List<string> { article.Id });
            visualizer.SetHCoreNodes(hCore);
            visualizer.DrawGraph(currentDisplayGraph, chkShowIdEdges.IsChecked ?? true);

            UpdateDisplayStats();

            txtResults.Text = $"🎯 Makale h-index Analizi\n\n" +
                             $"Makale: {article.ShortId}\n" +
                             $"Başlık: {article.Title}\n\n" +
                             $"h-index: {hIndex}\n" +
                             $"h-median: {hMedian:F1}\n\n" +
                             $"h-core ({hCore.Count} makale):\n" +
                             string.Join("\n", hCore.Take(15).Select(a =>
                                 $"• {a.ShortId} ({graph.GetCitations(a.Id).Count} atıf) - {(a.Title.Length > 35 ? a.Title.Substring(0, 35) + "..." : a.Title)}"));

            if (hCore.Count > 15)
            {
                txtResults.Text += $"\n... ve {hCore.Count - 15} makale daha";
            }
        }

        private void BtnFindHCore_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Önce veri yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtHValue.Text, out int h) || h < 1)
            {
                MessageBox.Show("Geçerli bir h değeri girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hCoreArticles = hCoreAlgorithm.FindHCore(h);

            txtGlobalHCoreInfo.Text = $"✅ h={h} için {hCoreArticles.Count} makale bulundu.";

            if (hCoreArticles.Count > 0)
            {
                var hCoreIds = new HashSet<string>(hCoreArticles.Select(a => a.Id));
                displayedNodeIds = hCoreIds;
                currentDisplayGraph = graph.CreateSubgraph(hCoreIds);

                visualizer.ClearHighlights();
                visualizer.SetHCoreNodes(hCoreArticles);
                visualizer.DrawGraph(currentDisplayGraph, chkShowIdEdges.IsChecked ?? true);

                UpdateDisplayStats();

                string resultText = $"📊 h={h} CORE ANALIZI\n" +
                                   $"{'=' * 40}\n\n" +
                                   $"Bulunan Makale Sayısı: {hCoreArticles.Count}\n" +
                                   $"Tanım: En az {h} atıf alan makale\n\n" +
                                   $"Makaleler:\n" +
                                   string.Join("\n", hCoreArticles.Take(15).Select(a =>
                                       $"• {a.ShortId} - {graph.GetCitations(a.Id).Count} atıf\n  {(a.Title.Length > 40 ? a.Title.Substring(0, 40) + "..." : a.Title)}"));

                if (hCoreArticles.Count > 15)
                {
                    resultText += $"\n\n... ve {hCoreArticles.Count - 15} makale daha";
                }

                txtResults.Text = resultText;
            }
            else
            {
                txtResults.Text = $"❌ h={h} için h-core bulunamadı.\nDaha düşük bir h değeri deneyin.";
            }
        }

        private void BtnFindKCore_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Önce veri yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtKValue.Text, out int k) || k < 1)
            {
                MessageBox.Show("Geçerli bir k değeri girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var undirectedGraph = graph.ConvertToUndirected();
            var undirectedKCore = new KCore(undirectedGraph);
            var kCoreArticles = undirectedKCore.GetKCoreArticles(k);

            txtKCoreInfo.Text = $"✅ k={k} için {kCoreArticles.Count} makale bulundu.";

            if (kCoreArticles.Count > 0)
            {
                var kCoreIds = new HashSet<string>(kCoreArticles.Select(a => a.Id));
                var kCoreGraph = undirectedGraph.CreateSubgraph(kCoreIds);

                displayedNodeIds = kCoreIds;
                currentDisplayGraph = graph.CreateSubgraph(kCoreIds);

                visualizer.ClearHighlights();
                visualizer.HighlightNodes(kCoreArticles.Select(a => a.Id).ToList());
                visualizer.DrawGraph(currentDisplayGraph, chkShowIdEdges.IsChecked ?? true);
                visualizer.DrawKCoreHighlight(kCoreGraph, k);

                UpdateDisplayStats();

                string resultText = $"🔗 k={k} CORE DECOMPOSITION (YÖNSÜZ GRAF)\n" +
                                   $"{'=' * 40}\n\n" +
                                   $"Bulunan Makale Sayısı: {kCoreArticles.Count}\n" +
                                   $"Degeneracy: {undirectedKCore.GetDegeneracy()}\n" +
                                   $"Tanım: En az {k} derece ile bağlı makalalar\n\n" +
                                   $"Makaleler:\n" +
                                   string.Join("\n", kCoreArticles.Take(15).Select(a =>
                                       $"• {a.ShortId} (derece: {undirectedGraph.GetUndirectedDegree(a.Id)})"));

                if (kCoreArticles.Count > 15)
                {
                    resultText += $"\n\n... ve {kCoreArticles.Count - 15} makale daha";
                }

                txtResults.Text = resultText;
            }
            else
            {
                txtResults.Text = $"❌ k={k} için k-core bulunamadı.\nDaha düşük bir k değeri deneyin.";
            }
        }

        private void BtnFindPath_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Önce veri yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var startItem = cmbStartArticle.SelectedItem as ComboBoxItem;
            var endItem = cmbEndArticle.SelectedItem as ComboBoxItem;

            if (startItem == null || endItem == null)
            {
                MessageBox.Show("Başlangıç ve hedef makale seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string startId = startItem.Tag.ToString();
            string endId = endItem.Tag.ToString();

            if (string.IsNullOrEmpty(startId) || string.IsNullOrEmpty(endId))
            {
                MessageBox.Show("Makale ID'leri boş!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var path = bfsAlgorithm.FindShortestPath(startId, endId);

            if (path != null && path.Count > 0)
            {
                txtPathInfo.Text = $"Yol uzunluğu: {path.Count - 1} adım";

                var pathIds = new HashSet<string>(path);
                var subgraph = graph.CreateSubgraph(pathIds);

                visualizer.ClearHighlights();
                visualizer.HighlightPath(path);
                visualizer.DrawGraph(subgraph, false);

                string pathStr = string.Join(" → ", path.Select(id =>
                {
                    var article = graph.GetArticle(id);
                    return article?.ShortId ?? "?";
                }));

                txtResults.Text = $"🛤️ En Kısa Yol (BFS)\n\n" +
                                 $"Başlangıç: {graph.GetArticle(startId)?.ShortId}\n" +
                                 $"Hedef: {graph.GetArticle(endId)?.ShortId}\n" +
                                 $"Yol Uzunluğu: {path.Count - 1} adım\n\n" +
                                 $"Yol:\n{pathStr}";
            }
            else
            {
                var startArticle = graph.GetArticle(startId);
                var endArticle = graph.GetArticle(endId);

                int startNeighbors = graph.GetNeighbors(startId).Count;
                int endNeighbors = graph.GetNeighbors(endId).Count;

                string debugInfo = $"Başlangıç: {startArticle?.ShortId ?? startId}\n" +
                                  $"Başlangıç Komşuları: {startNeighbors}\n\n" +
                                  $"Hedef: {endArticle?.ShortId ?? endId}\n" +
                                  $"Hedef Komşuları: {endNeighbors}\n\n" +
                                  (startNeighbors == 0 ? "⚠️ Başlangıç makalesi izole bir düğümdür.\n" : "") +
                                  (endNeighbors == 0 ? "⚠️ Hedef makalesi izole bir düğümdür.\n" : "") +
                                  "\n📌 Not: Seçilen iki makale aynı bağlı bileşende olmayabilir.";

                txtPathInfo.Text = "Yol bulunamadı!";
                txtResults.Text = $"Bu iki makale arasında bir yol bulunamadı.\n\n{debugInfo}";
            }
        }

        private void BtnCalculateBetweenness_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Önce veri yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (currentDisplayGraph == null || currentDisplayGraph.ArticleCount == 0)
            {
                MessageBox.Show("Önce bir graf görselleştirin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var undirectedGraph = currentDisplayGraph.ConvertToUndirected();
            var betweenness = new BetweennessCentrality(undirectedGraph);
            var allResults = betweenness.GetAllCentralNodes();

            // Sonuçları kaydet
            lastBetweennessResults = allResults;
            betweennessShowCount = 30;
            lastResultType = "betweenness";
            ShowBetweennessResults();
        }

        private List<(Article, double)> lastBetweennessResults = new List<(Article, double)>();
        private int betweennessShowCount = 30;

        private void ShowBetweennessResults()
        {
            if (lastBetweennessResults.Count == 0) return;

            string resultText = $"📊 BETWEENNESS CENTRALITY ANALIZI (Yönsüz Graf)\n" +
                               $"{'=' * 50}\n\n" +
                               $"Toplam Düğüm Sayısı: {lastBetweennessResults.Count}\n" +
                               $"Ortalama Merkezilik: {lastBetweennessResults.Average(x => x.Item2):F4}\n\n" +
                               $"En Yüksek Merkezilik Değerine Sahip Düğümler:\n\n";

            int showCount = Math.Min(betweennessShowCount, lastBetweennessResults.Count);
            for (int i = 0; i < showCount; i++)
            {
                var (article, centrality) = lastBetweennessResults[i];
                int citCount = graph.GetCitations(article.Id).Count;
                int refCount = graph.GetReferences(article.Id).Count;
                resultText += $"{i + 1}. {article.ShortId} (Merkezilik: {centrality:F6})\n";
                resultText += $"   {citCount} atıf, {refCount} referans\n\n";
            }

            if (lastBetweennessResults.Count > showCount)
            {
                resultText += $"\n... ve {lastBetweennessResults.Count - showCount} düğüm daha";
                txtShowMore.Text = "▼ Devamını Gör";
                txtShowMore.Visibility = Visibility.Visible;
            }
            else
            {
                txtShowMore.Visibility = Visibility.Collapsed;
            }

            txtResults.Text = resultText;
        }

        private void BtnFindComponents_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Önce veri yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            lastComponents = dfsAlgorithm.FindConnectedComponents();
            componentsShowCount = 20;
            lastResultType = "components";
            ShowComponents();
        }

        private void ShowComponents()
        {
            var sortedComponents = lastComponents.OrderByDescending(c => c.Count).ToList();
            int showCount = Math.Min(componentsShowCount, sortedComponents.Count);

            txtResults.Text = $"🔍 DFS - Bağlı Bileşenler\n\n" +
                             $"Toplam Bileşen Sayısı: {lastComponents.Count}\n\n";

            for (int i = 0; i < showCount; i++)
            {
                var component = sortedComponents[i];
                txtResults.Text += $"Bileşen {i + 1}: {component.Count} makale\n";
            }

            if (sortedComponents.Count > showCount)
            {
                txtShowMore.Visibility = Visibility.Visible;
            }
            else
            {
                txtShowMore.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtShowMore_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lastResultType == "betweenness" && lastBetweennessResults != null && lastBetweennessResults.Count > 0)
            {
                betweennessShowCount += 30;
                ShowBetweennessResults();
            }
            else if (lastResultType == "cycles" && lastCycles != null && lastCycles.Count > 0)
            {
                cyclesShowCount += 5;
                ShowCycles();
            }
            else if (lastComponents != null && lastComponents.Count > 0)
            {
                componentsShowCount += 20;
                ShowComponents();
            }
        }

        private void BtnDetectCycle_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Önce veri yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            lastCycles = dfsAlgorithm.FindAllCycles();
            cyclesShowCount = 5;
            lastResultType = "cycles";
            ShowCycles();
        }

        private void ShowCycles()
        {
            if (lastCycles == null) return;

            if (lastCycles.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("🔄 Döngü Tespiti\n");
                sb.AppendLine($"⚠️ Grafda {lastCycles.Count} döngü tespit edildi!\n");
                sb.AppendLine("Bu, bazı makalelerin birbirlerine karşılıklı atıf yaptığı anlamına gelir.\n");
                sb.AppendLine("Bulunan döngüler:");

                int showCount = Math.Min(lastCycles.Count, cyclesShowCount);
                for (int i = 0; i < showCount; i++)
                {
                    var cycle = lastCycles[i];
                    sb.AppendLine($"\nDöngü {i + 1}:");
                    foreach (var articleId in cycle)
                    {
                        var article = graph.GetArticle(articleId);
                        string shortId = articleId.Replace("https://openalex.org/W", "");
                        string title = article?.Title ?? "Bilinmiyor";
                        if (title.Length > 40) title = title.Substring(0, 40) + "...";
                        sb.AppendLine($"  → {shortId}: {title}");
                    }
                }

                if (lastCycles.Count > cyclesShowCount)
                    sb.AppendLine($"\n... ve {lastCycles.Count - cyclesShowCount} döngü daha");

                txtResults.Text = sb.ToString();
                txtShowMore.Visibility = lastCycles.Count > cyclesShowCount ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                txtResults.Text = "🔄 Döngü Tespiti\n\n" +
                                 "✅ Grafda döngü bulunamadı.\n\n" +
                                 "Graf bir DAG (Directed Acyclic Graph) yapısındadır.";
                txtShowMore.Visibility = Visibility.Collapsed;
            }
        }

        private void ChkShowIdEdges_Changed(object sender, RoutedEventArgs e)
        {
            if (graph != null && visualizer != null && currentDisplayGraph != null)
            {
                visualizer.DrawGraph(currentDisplayGraph, chkShowIdEdges.IsChecked ?? true);
            }
        }
    }
}
