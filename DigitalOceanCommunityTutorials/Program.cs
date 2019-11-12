using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Html2Markdown;
using Newtonsoft.Json.Linq;

namespace DigitalOceanCommunityTutorials
{
	class Program
	{
		static void Main(string[] args)
		{
			var client = new HttpClient
			{
				BaseAddress = new Uri("https://www.digitalocean.com/community/api/tutorials")
			};

			var initialPageResponse = Task.Run(
				() =>
				{
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
					return client.GetAsync(client.BaseAddress);
				}).GetAwaiter().GetResult();

			var pageContent = Task.Run(() => initialPageResponse.Content.ReadAsStringAsync()).GetAwaiter().GetResult();

			var parsedObject = JObject.Parse(pageContent);
			var linksObject = parsedObject.SelectToken("links");
			var pagesObject = linksObject.SelectToken("pages");
			var lastObject = pagesObject.SelectToken("last");
			var maxPages = Convert.ToInt32(Regex.Match(lastObject.Value<string>(), @"\d+").Value);

			for (int i = 0; i < maxPages; i++)
			{
				var pageResponse = Task.Run(
					() => client.GetStringAsync($"?page={i}")
				).GetAwaiter().GetResult();

				var parsed = JObject.Parse(pageResponse);

				foreach (var data in parsed.SelectTokens("data").Children())
				{
					var id = data.SelectToken("id").Value<int>();
					Console.WriteLine($"Processing: {id}");

					foreach (var attribute in data.SelectTokens("attributes"))
					{
						var title = attribute.SelectToken("title").Value<string>();
						var content = attribute.SelectToken("content").Value<string>();

						var markdownContent = ConvertToMarkdown(content);
						CreateFile(title, markdownContent);
					}
				}
			}
		}

		private static string ConvertToMarkdown(string content)
		{
			var converter = new Converter();
			try
			{
				Console.WriteLine("Converting to Markdown...");
				var markdown = converter.Convert(content);
				return markdown;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine("Couldn't convert, writing content instead");
				return content;
			}
		}

		private static void CreateFile(string title, string content)
		{
			string path = $@"Path\To\Your\Pages\{title}.md";

			try
			{
				Console.WriteLine("Writing file..");
				// Create the file, or overwrite if the file exists.
				using (FileStream fs = File.Create(path))
				{
					byte[] info = new UTF8Encoding(true).GetBytes(content);
					// Add some information to the file.
					fs.Write(info, 0, info.Length);
					Console.WriteLine("Written successfully");
				}
			}

			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
