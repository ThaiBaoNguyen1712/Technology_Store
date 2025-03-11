using System.Xml.Linq;
using Tech_Store.Models;

namespace Tech_Store.Services
{
    public class SitemapService
    {
        private readonly ApplicationDbContext _context;

        public SitemapService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void GenerateSitemap()
        {
            // Tạo đối tượng XML sitemap
            XElement urlset = new XElement(XName.Get("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9"));

            // Thêm các URL tĩnh của website (như trang chủ, giới thiệu, liên hệ, ...)
            urlset.Add(CreateUrlElement("/", 1.0, "daily"));
            urlset.Add(CreateUrlElement("/about", 0.8, "monthly"));
            urlset.Add(CreateUrlElement("/contact", 0.8, "monthly"));

            // Lấy các sản phẩm từ cơ sở dữ liệu
            var products = _context.Products.Select(p => new
            {
                p.Slug,
                p.UpdatedAt
            }).ToList();

            foreach (var product in products)
            {
                urlset.Add(CreateUrlElement($"/product/{product.Slug}", 0.9, "weekly", product.UpdatedAt));
            }

            // Lưu sitemap vào file XML
            var sitemapPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sitemap.xml");
            urlset.Save(sitemapPath);

            Console.WriteLine("Sitemap generated successfully.");
        }

        private XElement CreateUrlElement(string url, double priority, string changeFrequency, DateTime? lastMod = null)
        {
            var urlElement = new XElement("url",
                //Thay URL
                new XElement("loc", "https://techshop-c4cafccbh0dmcwdp.eastasia-01.azurewebsites.net/" + url),
                new XElement("priority", priority),
                new XElement("changefreq", changeFrequency));

            if (lastMod.HasValue)
            {
                urlElement.Add(new XElement("lastmod", lastMod.Value.ToString("yyyy-MM-dd")));
            }

            return urlElement;
        }
    }
}
