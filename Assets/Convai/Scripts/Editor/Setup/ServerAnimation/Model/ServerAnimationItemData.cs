namespace Convai.Scripts.Editor.Setup.ServerAnimation.Model {

    internal class ServerAnimationItemData {
        public bool CanBeSelected;
        public bool IsSelected;
        public ServerAnimationItemResponse ItemResponse;

        public bool IsPending => ItemResponse.Status == "pending";
        public bool IsSuccess => ItemResponse.Status == "success";
        public bool IsFailed => ItemResponse.Status == "failed";
    }

}