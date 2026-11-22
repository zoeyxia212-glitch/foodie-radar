using FoodieRadar.Api.Models;

namespace FoodieRadar.Api.Data;

/// <summary>
/// Seeds sample data on first run. All restaurant names and reviews are fictional.
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Restaurants.Any()) return;

        var now = DateTime.UtcNow;
        int day = 0;
        Review R(ReviewSource s, string author, string origin, int rating, string text, params string[] hl) => new()
        {
            Source = s, Author = author, ReviewerOrigin = origin, Rating = rating,
            Text = text, Highlights = hl.ToList(), CreatedAt = now.AddDays(-(++day))
        };

        const ReviewSource G = ReviewSource.Google, X = ReviewSource.Xiaohongshu,
                           Rd = ReviewSource.Reddit, O = ReviewSource.OnSite;

        var demo = new List<Restaurant>
        {
            new Restaurant
            {
                Name = "蜀味小馆", EnglishName = "Shu Taste Kitchen", Cuisine = "sichuan", Area = "North Shore",
                PriceLevel = 2, SpiceLevel = 4, Tastes = { "麻辣", "重口", "锅气" },
                MustOrder = { "水煮鱼", "口水鸡", "干煸四季豆" }, Avoid = { "宫保鸡丁" },
                Summary = "花椒够麻，四川评价者普遍认可；本地评价者多嫌太咸太油。",
                AiVerdict = "想吃正宗麻辣就来，宫保鸡丁是甜口改良版可以跳过。周五晚上排队较久。",
                Reviews =
                {
                    R(X, "成都人在奥克兰", "四川", 5, "终于在北岸找到花椒是真麻的川菜！水煮鱼的油是现泼的。", "花椒是真麻的", "水煮鱼"),
                    R(G, "Mike T.", "新西兰", 3, "Way too spicy and salty for me. Service was quick though.", "too spicy and salty"),
                    R(O, "Zoey", "中国", 4, "口水鸡必点。宫保鸡丁偏甜，像是给本地人改过的。", "口水鸡", "宫保鸡丁偏甜"),
                    R(Rd, "u/aklfoodie", "中国", 5, "Legit Sichuan. Ask for the Chinese menu, it has more dishes.", "Chinese menu"),
                }
            },
            new Restaurant
            {
                Name = "港湾茶餐厅", EnglishName = "Harbour Cha Chaan Teng", Cuisine = "canton", Area = "CBD",
                PriceLevel = 1, SpiceLevel = 0, Tastes = { "鲜", "锅气", "清淡" },
                MustOrder = { "干炒牛河", "丝袜奶茶", "菠萝油" }, Avoid = { "咖喱鱼蛋" },
                Summary = "香港评价者认可奶茶和干炒牛河；差评主要在服务，不在食物。",
                AiVerdict = "食物比综合分更好，差评大多是在说等位和服务态度。",
                Reviews =
                {
                    R(X, "HK仔", "香港", 5, "干炒牛河锅气十足，奶茶够滑，有香港的感觉。", "干炒牛河锅气十足"),
                    R(G, "Sarah L.", "英国", 2, "Staff were rude and we waited 20 mins for a table.", "waited 20 mins"),
                    R(O, "阿May", "广东", 4, "菠萝油正，鱼蛋就一般般啦。", "菠萝油"),
                }
            },
            new Restaurant
            {
                Name = "Kaze 炉端烧", EnglishName = "Kaze Robata", Cuisine = "japan", Area = "Newmarket",
                PriceLevel = 3, SpiceLevel = 0, Tastes = { "鲜", "清淡" },
                MustOrder = { "炙烤三文鱼腹", "鸡肉串" }, Avoid = { "拉面" },
                Summary = "本地评分很高，日本评价者认为调味偏甜，属于西式日料。",
                AiVerdict = "约会请客不错；想吃地道居酒屋的话，日本评价者打分偏低。",
                Reviews =
                {
                    R(G, "Yuki S.", "日本", 3, "刺身は新鮮。でもタレが少し甘すぎる。", "タレが少し甘すぎる"),
                    R(G, "James W.", "新西兰", 5, "Best date night spot! The salmon belly is incredible.", "salmon belly"),
                    R(X, "东京胃", "中国", 4, "环境满分，拉面不建议，汤底太淡。", "拉面不建议"),
                }
            },
            new Restaurant
            {
                Name = "Seoul Pocha", EnglishName = "首尔布帐马车", Cuisine = "korea", Area = "CBD",
                PriceLevel = 2, SpiceLevel = 3, Tastes = { "甜", "重口", "酸" },
                MustOrder = { "部队锅", "酱油炸鸡", "自家泡菜" }, Avoid = { "石锅拌饭" },
                Summary = "韩国留学生常去，部队锅和炸鸡获韩国评价者好评。",
                AiVerdict = "韩国人自己也来吃，适合 4 人以上聚餐。",
                Reviews =
                {
                    R(G, "Minji K.", "韩国", 5, "김치 직접 담근 맛! 부대찌개 진짜 맛있어요.", "김치 직접 담근 맛"),
                    R(X, "韩剧少女", "中国", 4, "酱油炸鸡比甜辣的好吃，拌饭一般。", "酱油炸鸡"),
                }
            },
            new Restaurant
            {
                Name = "Phở Mẹ", EnglishName = "Mum's Pho", Cuisine = "vietnam", Area = "Dominion Rd",
                PriceLevel = 1, SpiceLevel = 1, Tastes = { "鲜", "汤底浓", "清淡" },
                MustOrder = { "生熟牛河粉", "越式法包" },
                Summary = "越南评价者打分最高，牛骨汤底浓郁，性价比高。",
                AiVerdict = "本区正宗度第一，周末中午建议 11:30 前到。",
                Reviews =
                {
                    R(G, "Linh N.", "越南", 5, "Giống phở Hà Nội, nước dùng rất ngọt xương.", "Giống phở Hà Nội"),
                    R(Rd, "u/pho_hunter", "新西兰", 5, "Best pho in Auckland, $16 for a huge bowl.", "$16 for a huge bowl"),
                    R(O, "小林", "中国", 4, "汤底很浓，就是座位有点挤。", "汤底很浓"),
                }
            },
            new Restaurant
            {
                Name = "Bangkok Street", EnglishName = "曼谷街头", Cuisine = "thai", Area = "North Shore",
                PriceLevel = 2, SpiceLevel = 3, Tastes = { "酸", "甜", "麻辣" },
                MustOrder = { "泰式炒河粉", "绿咖喱" }, Avoid = { "冬阴功" },
                Summary = "本地人喜欢，泰国评价者认为太甜、不够辣。",
                AiVerdict = "本地口味友好，想吃地道泰餐可能会失望。",
                Reviews =
                {
                    R(G, "Ploy P.", "泰国", 3, "Too sweet, tom yum not sour enough. Not like Bangkok.", "Too sweet"),
                    R(G, "Emma R.", "新西兰", 5, "Love the green curry! Perfect spice level.", "green curry"),
                }
            },
            new Restaurant
            {
                Name = "Kopitiam 88", EnglishName = "咖啡店 88", Cuisine = "malay", Area = "Botany",
                PriceLevel = 1, SpiceLevel = 2, Tastes = { "汤底浓", "重口", "鲜" },
                MustOrder = { "咖喱叻沙", "椰浆饭", "海南鸡饭" }, Avoid = { "Roti" },
                Summary = "马来西亚评价者认可叻沙和椰浆饭，参巴酱自制。",
                AiVerdict = "东区性价比之选，叻沙最受好评。",
                Reviews =
                {
                    R(X, "槟城妹", "马来西亚", 5, "参巴酱是自己做的，叻沙汤头够浓！", "参巴酱是自己做的"),
                    R(G, "Daniel O.", "新西兰", 4, "Great laksa. Roti was a bit oily.", "Roti was a bit oily"),
                }
            },
            new Restaurant
            {
                Name = "老成都串串", EnglishName = "Old Chengdu Skewers", Cuisine = "sichuan", Area = "Dominion Rd",
                PriceLevel = 2, SpiceLevel = 5, Tastes = { "麻辣", "重口", "汤底浓" },
                MustOrder = { "牛油锅底", "郡肝", "藤椒鸡" },
                Summary = "综合分偏低是因为本地人吃不了这么辣；中国评价者给分很高。",
                AiVerdict = "综合评分严重低估，只看同乡评价分数高很多。不能吃辣慎入。",
                Reviews =
                {
                    R(X, "重庆火锅控", "重庆", 5, "牛油锅底是正经炒的，不是那种料包味。", "牛油锅底"),
                    R(G, "Tom H.", "澳大利亚", 2, "My mouth was on fire for an hour. Not for me.", "on fire"),
                    R(O, "Zoey", "中国", 5, "藤椒鸡超级惊喜，下次还来！", "藤椒鸡"),
                }
            }
        };

        // Demo rows use a suburb name in Area; convert it to a region.
        var regionOf = new Dictionary<string, string>
        {
            ["North Shore"] = "North Shore",
            ["CBD"] = "Central Auckland",
            ["Dominion Rd"] = "Central Auckland",
            ["Newmarket"] = "Central Auckland",
            ["Botany"] = "East Auckland",
        };
        foreach (var r in demo)
        {
            r.Suburb = r.Area;
            r.Area = regionOf.GetValueOrDefault(r.Area, "Central Auckland");
            r.IsDemo = true;
            r.IsCurated = true;
        }

        db.Restaurants.AddRange(demo);
        db.SaveChanges();
    }
}
