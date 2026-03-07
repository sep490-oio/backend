using Bogus;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Infrastructure.Persistence.Seed;

public static partial class FakeDataSeeder
{
    private static void SeedQuestions(
        List<Item> items,
        List<User> users,
        int maxQuestionsPerItem,
        DateTime nowUtc)
    {
        var bidders = users
            .Where(u => u.UserName.Value.StartsWith("bidder"))
            .ToList();

        var sellers = users
            .Where(u => u.UserName.Value.StartsWith("seller"))
            .ToList();

        var faker = new Faker("vi");

        var sampleQuestions = new[]
        {
            "Sản phẩm còn bảo hành không?",
            "Có ship COD không?",
            "Sản phẩm có bị trầy xước gì không?",
            "Phụ kiện đi kèm gồm những gì?",
            "Đã sử dụng bao lâu rồi?",
            "Có hỗ trợ đổi trả không?",
            "Pin còn tốt không? Dùng được bao lâu?",
            "Có xuất hóa đơn không?",
            "Giao hàng mất bao lâu?",
            "Có thể xem hàng trực tiếp không?",
            "Giá cuối cùng là bao nhiêu?",
            "Có hỗ trợ trả góp không?",
            "Sản phẩm còn nguyên seal không?",
            "Có thể giảm giá thêm không?",
            "Lý do bán sản phẩm này là gì?",
        };

        var sampleAnswers = new[]
        {
            "Dạ còn bảo hành chính hãng 6 tháng.",
            "Có hỗ trợ ship COD toàn quốc.",
            "Sản phẩm còn rất mới, không có trầy xước.",
            "Đầy đủ phụ kiện: sạc, cáp, tai nghe, hộp.",
            "Mới mua được 3 tháng.",
            "Có hỗ trợ đổi trả trong 7 ngày.",
            "Pin rất tốt, sử dụng được cả ngày.",
            "Có xuất hóa đơn đầy đủ.",
            "Giao hàng trong 2-3 ngày làm việc.",
            "Có thể xem hàng tại HCM.",
            "Giá đã bao gồm phí ship.",
            "Hiện tại chưa hỗ trợ trả góp.",
            "Sản phẩm đã khui seal nhưng còn rất mới.",
            "Giá đã là tốt nhất rồi ạ.",
            "Lên đời nên bán lại.",
        };

        var activeItems = items
            .Where(i => i.Status == ItemStatus.Active || i.Status == ItemStatus.InAuction)
            .ToList();

        foreach (var item in activeItems)
        {
            var questionCount = faker.Random.Int(0, 5);

            for (var i = 0; i < questionCount; i++)
            {
                var asker = faker.PickRandom(bidders);

                // Don't ask own item
                if (asker.Id == item.SellerId) continue;

                try
                {
                    var question = item.AskQuestion(
                        asker.Id,
                        faker.PickRandom(sampleQuestions),
                        maxQuestionsPerItem, 
                        nowUtc);

                    // 70% chance seller answers
                    if (faker.Random.Bool(0.7f))
                    {
                        item.AnswerQuestion(question.Value.Id, faker.PickRandom(sampleAnswers), nowUtc);
                    }
                }
                catch
                {
                    // Skip if max questions reached
                }
            }
        }
    }
}