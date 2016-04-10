﻿

namespace HabitatApp.Services
{
	using System;
	using Xamarin.Forms;
	using System.Threading.Tasks;
	using Sitecore.MobileSDK.API.Items;
	using System.Linq;
	using Sitecore.MobileSDK.API.Request.Parameters;
	using System.Collections.Generic;
	using HabitatApp.Repositories;
	using HabitatApp.Models;
	using Autofac;
	using HabitatApp.ViewModels;

	public class NavigationService : INavigationService
	{

		private readonly ISitecoreService _sitecoreService;
		private readonly ISettingsRepository _settingsRepository;

		public NavigationService (ISitecoreService sitecoreService, ISettingsRepository settingsRepository)
		{
			_sitecoreService = sitecoreService;
			_settingsRepository = settingsRepository;
		}

		private Page LoadPageByPageData(PageData pageData)
		{

			if (pageData == null || pageData.ItemContext == null)
				return null;

			ISitecoreItem item = pageData.ItemContext.First ();

			if(item == null)
				return null;


			Type pageType = Type.GetType ($"HabitatApp.Views.Pages.{item.GetTemplateName ()}");

			if (pageType == null)
				return null;

			//Load page by page type
			Page currentPage = HabitatApp.App.Instance.Container.Resolve(pageType) as Page;
		
			IViewModel viewModel = (IViewModel)currentPage.BindingContext;

			viewModel.PageContext = pageData;

			viewModel.ConnectedToPage = currentPage;

			return currentPage;

		}

		private async Task<Page> LoadPageByItemId(string id)
		{

			Settings settings = await _settingsRepository.GetWithFallback ();

			PageData pageData = await _sitecoreService.GeneratePageData(id, 
				PayloadType.Content, 
				new List<ScopeType>(){ ScopeType.Self }, 
				Constants.Sitecore.Fields.Teasers.AccordeonSelector, 
				settings.SitecoreDefaultLanguage);


			return LoadPageByPageData(pageData);

		}


		public async Task NavigateToPageByItemId (Page navigateFromPage, string itemId)
		{
			Page page = await LoadPageByItemId(itemId);
			await NavigateAndLoadBindingContext (navigateFromPage, page);
		}

		public async Task NavigateToPageByPageData (Page navigateFromPage, PageData pageData)
		{
			Page page = LoadPageByPageData(pageData);
			await NavigateAndLoadBindingContext (navigateFromPage, page);
		}

	

		private async Task NavigateAndLoadBindingContext(Page navigateFromPage, Page navigateToPage){

			IViewModel viewModel = (IViewModel)navigateToPage.BindingContext;

			//We need to load it all before page appears
			//Page.Appering() gives an unfortunate delay
			await viewModel.LoadAsync ();

			await navigateFromPage.Navigation.PushAsync (navigateToPage);
		}



	}
}
