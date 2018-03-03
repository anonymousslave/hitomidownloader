using System.Threading.Tasks;
using hitomiDownloader.Models;
namespace hitomiDownloader.Core
{
    public partial class Hitomi
    {
        public async void InitializeGallery()
        {
            await DatabaseManager.Instance.InitializeDatabase();
            await Galleries.Search();
        }
        public async Task UpdateGalleryCache()
        {
                NotificationManager.NotifyInformation("캐시 데이터 업데이트 시작");

                NotificationManager.NotifyInformation("태그 목록 업데이트 시작");
                await DatabaseManager.Instance.UpdateTags();
                NotificationManager.NotifyInformation("태그 목록 업데이트 완료");

                NotificationManager.NotifyInformation("메타데이터 업데이트 시작");
                await DatabaseManager.Instance.UpdateMetadata();
                NotificationManager.NotifyInformation("메타데이터 업데이트 완료");

                NotificationManager.NotifySuccess("캐시 데이터 업데이트 완료");
                NotificationManager.NotifyInformation("목록 갱신 시작");
                await Galleries.Search();
                NotificationManager.NotifySuccess("목록 갱신 완료");
        }
    }
}
