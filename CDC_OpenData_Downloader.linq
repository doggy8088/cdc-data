<Query Kind="Program">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
  <AutoDumpHeading>true</AutoDumpHeading>
</Query>

async Task Main()
{
	var currentPath = Environment.CurrentDirectory;

#if CMD
   // "I'm been called from lprun!".Dump();
#else
	var basepath = @"G:\cdc-data";
	System.IO.Directory.CreateDirectory(basepath);
	System.IO.Directory.SetCurrentDirectory(basepath);
	var button = new LINQPad.Controls.Button("開啟資料夾");
	button.Click += (sender, args) => System.Diagnostics.Process.Start("explorer.exe", $"\"{currentPath}\"");
	Util.HorizontalRun("當前目錄,動作", currentPath, button).Dump();
#endif

	await Task.Yield();

	// 請幫我下載 https://data.cdc.gov.tw/doc/OpdDataSetStatistics.json 檔案
	var OpdDataSetStatisticsJson = await DownloadFileAsync("https://data.cdc.gov.tw/doc/OpdDataSetStatistics.json");

	var datasets = JsonSerializer.Deserialize<List<Dataset>>(OpdDataSetStatisticsJson);
	datasets.Dump(depth: 0);

	int i = 0;

	List<資料集下載> links = new();

	foreach (var dataset in datasets.Where(d => d.資料集識別碼 != "None"))
	{
		i++;

		var packageUrl = $"https://data.cdc.gov.tw/api/3/action/package_show?id={dataset.資料集網址}";
		var json = await DownloadFileAsync(packageUrl.Dump($"{i:000}/{datasets.Count}: 正在下載 Package 中繼資料 - {dataset.資料集中文名}"));

		if (json is null)
		{
			continue;
		}

		var manifest = $"data/{GetSafeFilename(dataset.資料集中文名)}/manifest.json";

		bool forceDownload = false;

		if (!File.Exists(manifest))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(manifest));
			await File.WriteAllTextAsync(manifest, json);
			forceDownload = true;
		}

		var remoteManifest = JsonSerializer.Deserialize<PackageShowResult>(json);

		//continue;

		if (remoteManifest.Success)
		{
			var localjson = File.ReadAllText(manifest);
			var localManifest = JsonSerializer.Deserialize<PackageShowResult>(localjson);

			// TODO: 理論上應該要先清空 manifest 資料夾下所有檔案，除了 manifest.json 檔案

			// 刪除 filename 所在資料夾中的所有檔案，除了 manifest.json 檔案
			//var directory = Path.GetDirectoryName(manifest);
			//if (Directory.Exists(directory))
			//{
			//	var files = Directory.GetFiles(directory);
			//	foreach (var file in files)
			//	{
			//		if (Path.GetFileName(file) != "manifest.json")
			//		{
			//			File.Delete(file);
			//		}
			//	}
			//}

			//result.Result.CTitle;
			for (int resourceIndex = 0; resourceIndex < remoteManifest.Result.Resources.Length; resourceIndex++)
			{
				for (int resourceIndexLocal = 0; resourceIndexLocal < localManifest.Result.Resources.Length; resourceIndexLocal++)
				{
					var resource = remoteManifest.Result.Resources[resourceIndex];
					var resourceLocal = localManifest.Result.Resources[resourceIndexLocal];

					// 跟本地快取做比對，看資料有沒有更新
					if (resource.Id == resourceLocal.Id)
					{
						// 大部分都是這種格式
						// "format": "CSV",
						// "format": "JSON",

						// 少數有些例外格式
						// https://data.cdc.gov.tw/api/3/action/package_show?id=antibiotic-resistance-surveillance-data-salmonella
						// "format": "CSV UTF-8 (逗號分隔)(*.csv)檔",
						var format = resource.Format.ToLower().Split(' ')[0];

						// TODO: 先刻意跳過 JSON 檔案，因為有幾個檔案太大，且不好分段。等可以分段就加回來。
						if (format == "json")
						{
							continue;
						}

						// 還有這種 XML 格式，但是連結卻是 7z 的檔案 (What The Fuck!)
						// "format": "XML",
						var urlExt = Path.GetExtension(resource.Url.LocalPath);

						if (urlExt.TrimStart('.') != format)
						{
							format = urlExt.TrimStart('.');
						}

						var filename = $"data/{GetSafeFilename(dataset.資料集中文名)}/{GetSafeFilename(resource.Name)}.{format}";

						links.Add(new 資料集下載()
						{
							資料集名稱 = remoteManifest.Result.CTitle,
							資源名稱 = resource.Name,
							資源描述 = resource.Description,
							資源格式 = resource.Format,
							檔案路徑 = filename,
							分段檔案清單 = GetPartsFilePath(filename)
						});

						// 如果 metadata_modified 沒有修改，就跳過下載檔案
						if (!forceDownload)
						{
							if (localManifest.Result.MetadataModified == remoteManifest.Result.MetadataModified
								&& resource.MetadataModified == resourceLocal.MetadataModified)
							{
								$"資源「{resource.Name}」沒有更新，無須下載".Dump();
								continue;
							}

						}

						$"{resource.Url.ToString()}\r\n{filename}".Dump("正在下載檔案...");

						// TODO: 若要重新下載檔案，需先刪除 _part* 檔案
						var fileExt = Path.GetExtension(filename);
						var existing_part_files = Directory.GetFiles(Path.GetDirectoryName(filename), $"{Path.GetFileNameWithoutExtension(filename)}_part*{fileExt}");
						foreach (var file in existing_part_files)
						{
							$"刪除舊的 {file} 檔案".Dump("正在下載檔案...");
							File.Delete(file);
						}

						$"正在下載中...".Dump("正在下載檔案...");
						await DownloadFileAsync(resource.Url.ToString(), filename);
						$"下載完成。".Dump("正在下載檔案...");

						if (format == "csv")
						{
							string[] new_part_files = await SplitCsvFile(filename);

							links.Last().分段檔案清單 = new_part_files;
						}
					}
				}
			}

			await File.WriteAllTextAsync(manifest, json, new UTF8Encoding(true));
		}
	}

	// Open README.md and edit it's content. Update 最近更新時間: `2024-07-16 10:48:32` to latest time
	var readmePath = Path.Combine(currentPath, "README.md");
	if (File.Exists(readmePath))
	{
		var now = DateTime.UtcNow.AddHours(8); // Taiwan Time

		var readmeContent = await File.ReadAllTextAsync(readmePath);

		readmeContent = System.Text.RegularExpressions.Regex.Replace(readmeContent,
			@"最近更新時間: `\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}`",
			$"最近更新時間: `{now:yyyy-MM-dd HH:mm:ss}`");

		readmeContent = System.Text.RegularExpressions.Regex.Replace(readmeContent,
			@"## 資料下載(.*)## 相關連結",
			$"## 資料下載\r\n\r\n{GenerateLinksForDatasets(links)}\r\n\r\n## 相關連結",
			RegexOptions.Singleline);

		await File.WriteAllTextAsync(readmePath, readmeContent);
	}
}

string[] GetPartsFilePath(string filename)
{
	if (File.Exists(filename))
	{
		return new string[] { filename };
	}

	var fileExt = Path.GetExtension(filename);

	// 取得所有 *_partN.csv
	var part_files = Directory.GetFiles(Path.GetDirectoryName(filename), $"{Path.GetFileNameWithoutExtension(filename)}_part*{fileExt}");

	if (part_files.Length > 0)
	{
		return part_files;
	}
	else
	{
		return new string[] { filename };
		//throw new FileNotFoundException("找不到下載檔案", filepath);
	}
}

void GenerateLinksForDatasetsTest1()
{
	var readmePath = Path.Combine(Environment.CurrentDirectory, "README.md");
	if (File.Exists(readmePath))
	{
		var data = new List<UserQuery.資料集下載>();
		data.Add(new 資料集下載() { 資料集名稱 = "AA", 資源名稱 = "BB", 檔案路徑 = "https://doggy8088.github.io/cdc-data/data/%e7%96%be%e7%97%85%e7%ae%a1%e5%88%b6%e7%bd%b2%e8%b3%87%e6%96%99%e9%96%8b%e6%94%be%e5%b9%b3%e5%8f%b0/a.json" });
		data.Add(new 資料集下載() { 資料集名稱 = "AA", 資源名稱 = "DD", 檔案路徑 = "https://doggy8088.github.io/cdc-data/data/%e7%96%be%e7%97%85%e7%ae%a1%e5%88%b6%e7%bd%b2%e8%b3%87%e6%96%99%e9%96%8b%e6%94%be%e5%b9%b3%e5%8f%b0/a.json" });
		data.Add(new 資料集下載() { 資料集名稱 = "BB", 資源名稱="CC", 檔案路徑="https://doggy8088.github.io/cdc-data/data/%e7%96%be%e7%97%85%e7%ae%a1%e5%88%b6%e7%bd%b2%e8%b3%87%e6%96%99%e9%96%8b%e6%94%be%e5%b9%b3%e5%8f%b0/a.json"});
		GenerateLinksForDatasets(data).Dump();
	}
	return;
}

string GenerateLinksForDatasets(List<資料集下載> list)
{
	StringBuilder sb = new();

	var groups = list.GroupBy(l => l.資料集名稱);

	foreach (var group in groups.ToList())
	{
		sb.AppendLine($"### {group.Key}");
		sb.AppendLine();

		sb.AppendLine("| 檔案下載 | 資源名稱 | 資源描述 | 資源格式 |");
		sb.AppendLine("| -------- | -------- | -------- | -------- |");

		foreach (var item in group)
		{
			StringBuilder sbLinks = new();

			var basePath = Path.GetDirectoryName(item.檔案路徑).Replace("\\", "/");
			var mainFile = Path.GetFileName(item.檔案路徑);
			var mainUrl = $"https://doggy8088.github.io/cdc-data/{System.Web.HttpUtility.UrlPathEncode(basePath + "/" + mainFile)}";

			sbLinks.Append($"[Download]({mainUrl})");

			if (item.分段檔案清單.Length > 1)
			{
				sbLinks.Length = 0;

				for (int i = 0; i < item.分段檔案清單.Length; i++)
				{
					var partFile = Path.GetFileName(item.分段檔案清單[i]);
					var partUrl = $"https://doggy8088.github.io/cdc-data/{System.Web.HttpUtility.UrlPathEncode(basePath + "/" + partFile)}";

					sbLinks.Append($"[Part {i+1}]({partUrl})<br>");
				}
			}

			sb.AppendLine($"| {sbLinks.ToString()} | {item.資源名稱} | {item.資源描述.Trim().Replace("\r", "").Replace("\n", "<br>")} | {item.資源格式} |");
		}

		sb.AppendLine();
	}

	return sb.ToString();
}

public class 資料集下載
{
	public string 資料集名稱 { get; set; }
	public string 資源名稱 { get; set; }
	public string 資源描述 { get; set; }
	public string 資源格式 { get; set; }
	public string 檔案路徑 { get; set; }
	public string[] 分段檔案清單 { get; set; }
}

async Task<string[]> SplitCsvFile(string file)
{
	List<string> files = new();

	var fileInfo = new FileInfo(file);
	if (fileInfo.Length > 50_000_000) // ~50MB
	{
		file.Dump();//continue;
		using (var reader = new StreamReader(file))
		{
			int partNumber = 1;
			while (!reader.EndOfStream)
			{
				var partFileName = Path.Combine(fileInfo.DirectoryName, $"{Path.GetFileNameWithoutExtension(file)}_part{partNumber}.csv");

				$"正在加入分段檔案 {partFileName}".Dump("檔案分段");
				files.Add(partFileName);

				using (var writer = new StreamWriter(partFileName))
				{
					long currentSize = 0;
					while (currentSize < 50_000_000 && !reader.EndOfStream)
					{
						var line = await reader.ReadLineAsync();
						await writer.WriteLineAsync(line);
						currentSize += Encoding.UTF8.GetByteCount(line);
					}
				}

				partNumber++;
			}
		}
		$"正在刪除 {file} 檔案".Dump("檔案分段");
		File.Delete(file);

		return files.ToArray();
	}
	else
	{
		return new string[] { file };
	}
}

string GetSafeFilename(string name)
{
	return string.Join("_", name.Trim().Split(Path.GetInvalidFileNameChars()));
}

async Task<string> DownloadFileAsync(string url)
{
	using var client = new HttpClient();

	var response = await client.GetAsync(url);

	if (response.StatusCode == HttpStatusCode.Forbidden)
	{
		$"權限不足，無法下載 {url} 資料集內容".Dump();
		return null;
	}

	response.EnsureSuccessStatusCode();

	return await response.Content.ReadAsStringAsync();
}

async Task DownloadFileAsync(string url, string filename)
{
	using var client = new HttpClient();

	var response = await client.GetAsync(url);
	response.EnsureSuccessStatusCode();

	Directory.CreateDirectory(Path.GetDirectoryName(filename));

	using var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
	await response.Content.CopyToAsync(fileStream);
}

public class Dataset
{
	public string 負責單位 { get; set; }
	public string 資料集網址 { get; set; }
	public string 資料集中文名 { get; set; }
	public string 提供格式 { get; set; }
	public int 總瀏覽量 { get; set; }
	public int 總下載量 { get; set; }
	public string 資料集識別碼 { get; set; }
}

public partial class PackageShowResult
{
	[JsonPropertyName("help")]
	public Uri Help { get; set; }

	[JsonPropertyName("success")]
	public bool Success { get; set; }

	[JsonPropertyName("result")]
	public Result Result { get; set; }
}

public partial class Result
{
	[JsonPropertyName("author")]
	public string Author { get; set; }

	[JsonPropertyName("author_email")]
	public string AuthorEmail { get; set; }

	[JsonPropertyName("author_phone")]
	public string AuthorPhone { get; set; }

	[JsonPropertyName("c_title")]
	public string CTitle { get; set; }

	[JsonPropertyName("categoryDataset")]
	public string CategoryDataset { get; set; }

	[JsonPropertyName("categoryService")]
	public string CategoryService { get; set; }

	[JsonPropertyName("categoryTheme")]
	public string CategoryTheme { get; set; }

	[JsonPropertyName("cd_notes")]
	public string CdNotes { get; set; }

	[JsonPropertyName("cm_notes")]
	public string CmNotes { get; set; }

	[JsonPropertyName("creator_user_id")]
	public Guid CreatorUserId { get; set; }

	[JsonPropertyName("data_lang")]
	public string DataLang { get; set; }

	[JsonPropertyName("data_type")]
	public string DataType { get; set; }

	[JsonPropertyName("detectFrequency")]
	public string DetectFrequency { get; set; }

	[JsonPropertyName("e_title")]
	public string ETitle { get; set; }

	[JsonPropertyName("ea_author")]
	public string EaAuthor { get; set; }

	[JsonPropertyName("ed_notes")]
	public string EdNotes { get; set; }

	[JsonPropertyName("em_notes")]
	public string EmNotes { get; set; }

	[JsonPropertyName("fee")]
	public string Fee { get; set; }

	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("isopen")]
	public bool Isopen { get; set; }

	[JsonPropertyName("license_id")]
	public string LicenseId { get; set; }

	[JsonPropertyName("license_title")]
	public string LicenseTitle { get; set; }

	[JsonPropertyName("maintainer")]
	public object Maintainer { get; set; }

	[JsonPropertyName("maintainer_email")]
	public object MaintainerEmail { get; set; }

	[JsonPropertyName("metadata_created")]
	public DateTimeOffset MetadataCreated { get; set; }

	[JsonPropertyName("metadata_modified")]
	public DateTimeOffset MetadataModified { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("notes")]
	public object Notes { get; set; }

	[JsonPropertyName("num_resources")]
	public long NumResources { get; set; }

	[JsonPropertyName("num_tags")]
	public long NumTags { get; set; }

	[JsonPropertyName("organization")]
	public Organization Organization { get; set; }

	[JsonPropertyName("owner_org")]
	public Guid OwnerOrg { get; set; }

	[JsonPropertyName("private")]
	public bool Private { get; set; }

	[JsonPropertyName("state")]
	public string State { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; }

	[JsonPropertyName("updated_freq")]
	public string UpdatedFreq { get; set; }

	[JsonPropertyName("url")]
	public string Url { get; set; }

	[JsonPropertyName("version")]
	public object Version { get; set; }

	[JsonPropertyName("groups")]
	public Group[] Groups { get; set; }

	[JsonPropertyName("resources")]
	public Resource[] Resources { get; set; }

	[JsonPropertyName("tags")]
	public Tag[] Tags { get; set; }

	[JsonPropertyName("relationships_as_subject")]
	public object[] RelationshipsAsSubject { get; set; }

	[JsonPropertyName("relationships_as_object")]
	public object[] RelationshipsAsObject { get; set; }
}

public partial class Group
{
	[JsonPropertyName("description")]
	public string Description { get; set; }

	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; }

	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("image_display_url")]
	public string ImageDisplayUrl { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; }
}

public partial class Organization
{
	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; }

	[JsonPropertyName("description")]
	public string Description { get; set; }

	[JsonPropertyName("image_url")]
	public string ImageUrl { get; set; }

	[JsonPropertyName("created")]
	public DateTimeOffset Created { get; set; }

	[JsonPropertyName("is_organization")]
	public bool IsOrganization { get; set; }

	[JsonPropertyName("approval_status")]
	public string ApprovalStatus { get; set; }

	[JsonPropertyName("state")]
	public string State { get; set; }
}

public partial class Resource
{
	[JsonPropertyName("cache_last_updated")]
	public object CacheLastUpdated { get; set; }

	[JsonPropertyName("cache_url")]
	public object CacheUrl { get; set; }

	[JsonPropertyName("created")]
	public DateTimeOffset Created { get; set; }

	[JsonPropertyName("datastore_active")]
	public bool DatastoreActive { get; set; }

	[JsonPropertyName("description")]
	public string Description { get; set; }

	[JsonPropertyName("format")]
	public string Format { get; set; }

	[JsonPropertyName("hash")]
	public string Hash { get; set; }

	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("last_modified")]
	public object LastModified { get; set; }

	[JsonPropertyName("metadata_modified")]
	public DateTimeOffset MetadataModified { get; set; }

	[JsonPropertyName("mimetype")]
	public object Mimetype { get; set; }

	[JsonPropertyName("mimetype_inner")]
	public object MimetypeInner { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("package_id")]
	public Guid PackageId { get; set; }

	[JsonPropertyName("position")]
	public long Position { get; set; }

	[JsonPropertyName("resource_type")]
	public object ResourceType { get; set; }

	[JsonPropertyName("size")]
	public object Size { get; set; }

	[JsonPropertyName("state")]
	public string State { get; set; }

	[JsonPropertyName("url")]
	public Uri Url { get; set; }

	[JsonPropertyName("url_type")]
	public object UrlType { get; set; }
}

public partial class Tag
{
	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; }

	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("state")]
	public string State { get; set; }

	[JsonPropertyName("vocabulary_id")]
	public object VocabularyId { get; set; }
}