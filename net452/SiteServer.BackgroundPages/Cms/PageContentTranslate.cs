﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.BackgroundPages.Core;
using SiteServer.BackgroundPages.Utils;
using SiteServer.CMS.Caches.Content;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Enumerations;
using SiteServer.CMS.Fx;

namespace SiteServer.BackgroundPages.Cms
{
	public class PageContentTranslate : BasePageCms
    {
        public Literal LtlContents;
        public Button BtnTranslateAdd;
        public RadioButtonList RblTranslateType;

        private Dictionary<int, List<int>> _idsDictionary = new Dictionary<int, List<int>>();
        private string _returnUrl;

        public static string GetRedirectUrl(int siteId, int channelId, string returnUrl)
        {
            return FxUtils.GetCmsUrl(siteId, nameof(PageContentTranslate), new NameValueCollection
            {
                {"channelId", channelId.ToString()},
                {"ReturnUrl", StringUtils.ValueToUrl(returnUrl)}
            });
        }

        public static string GetRedirectClickStringForMultiChannels(int siteId, string returnUrl)
        {
            var redirectUrl = FxUtils.GetCmsUrl(siteId, nameof(PageContentTranslate), new NameValueCollection
            {
                {"ReturnUrl", StringUtils.ValueToUrl(returnUrl)}
            });
            return PageUtils.GetRedirectStringWithCheckBoxValue(redirectUrl, "IDsCollection", "IDsCollection", "请选择需要转移的内容！");
        }

        public static string GetRedirectClickString(int siteId, int channelId, string returnUrl)
        {
            var redirectUrl = FxUtils.GetCmsUrl(siteId, nameof(PageContentTranslate), new NameValueCollection
            {
                {"channelId", channelId.ToString()},
                {"ReturnUrl", StringUtils.ValueToUrl(returnUrl)}
            });
            return PageUtils.GetRedirectStringWithCheckBoxValue(redirectUrl, "contentIdCollection", "contentIdCollection", "请选择需要转移的内容！");
        }

		public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

			WebPageUtils.CheckRequestParameter("siteId");
            _returnUrl = StringUtils.ValueFromUrl(AuthRequest.GetQueryString("ReturnUrl"));
            if (string.IsNullOrEmpty(_returnUrl))
            {
                _returnUrl = AdminPagesUtils.Cms.GetContentsUrl(SiteId, AuthRequest.GetQueryInt("channelId"));
            }
            //if (!base.HasChannelPermissions(this.channelId, AppManager.CMS.Permission.Channel.ContentTranslate))
            //{
            //    WebPageUtils.RedirectToErrorPage("您没有此栏目的内容转移权限！");
            //    return;
            //}

            //bool isCut = base.HasChannelPermissions(this.channelId, AppManager.CMS.Permission.Channel.ContentDelete);
            const bool isCut = true;
            _idsDictionary = ContentUtility.GetIDsDictionary(Request.QueryString);

            if (IsPostBack) return;

            var builder = new StringBuilder();
            foreach (var channelId in _idsDictionary.Keys)
            {
                var contentIdList = _idsDictionary[channelId];
                if (contentIdList != null)
                {
                    foreach (var contentId in contentIdList)
                    {
                        var contentInfo = ContentManager.GetContentInfo(SiteInfo, channelId, contentId);
                        if (contentInfo != null)
                        {
                            builder.Append(
                                $@"{WebUtils.GetContentTitle(SiteInfo, contentInfo, _returnUrl)}<br />");
                        }
                    }
                }
            }
            LtlContents.Text = builder.ToString();

            BtnTranslateAdd.Attributes.Add("onclick", ModalChannelMultipleSelect.GetOpenWindowString(SiteId, true));

            SystemWebUtils.AddListItemsToETranslateContentType(RblTranslateType, isCut);
            SystemWebUtils.SelectSingleItem(RblTranslateType, ETranslateContentTypeUtils.GetValue(ETranslateContentType.Copy));
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            if (!Page.IsPostBack || !Page.IsValid) return;

            if (!string.IsNullOrEmpty(Request.Form["translateCollection"]))
            {
                try
                {
                    var translateType = ETranslateContentTypeUtils.GetEnumType(RblTranslateType.SelectedValue);

                    foreach (var channelId in _idsDictionary.Keys)
                    {
                        var contentIdList = _idsDictionary[channelId];
                        if (contentIdList != null)
                        {
                            contentIdList.Reverse();
                            if (contentIdList.Count > 0)
                            {
                                foreach (var contentId in contentIdList)
                                {
                                    ContentManager.Translate(SiteInfo, channelId, contentId, Request.Form["translateCollection"], translateType, AuthRequest.AdminName);

                                    AuthRequest.AddSiteLog(SiteInfo.Id, channelId, contentId, "转移内容", string.Empty);
                                }
                            }
                        }
                    }

                    SuccessMessage("内容转移成功！");
                    AddWaitAndRedirectScript(_returnUrl);
                }
                catch (Exception ex)
                {
                    LogUtils.AddErrorLog(ex);
                    FailMessage(ex, "内容转移失败！");
                }
            }
            else
            {
                FailMessage("请选择需要转移到的栏目！");
            }
        }

        public void Return_OnClick(object sender, EventArgs e)
        {
            WebPageUtils.Redirect(_returnUrl);
        }
	}
}
