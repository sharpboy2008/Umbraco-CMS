﻿using System;
using AutoMapper;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.Models.Mapping;

namespace Umbraco.Web.WebApi.Binders
{
    internal class MediaItemBinder : ContentItemBaseBinder<IMedia, MediaItemSave>
    {
        protected override IMedia GetExisting(MediaItemSave model)
        {
            return Services.MediaService.GetById(Convert.ToInt32(model.Id));
        }

        protected override IMedia CreateNew(MediaItemSave model)
        {
            var contentType = Services.MediaTypeService.Get(model.ContentTypeAlias);
            if (contentType == null)
            {
                throw new InvalidOperationException("No content type found wth alias " + model.ContentTypeAlias);
            }
            return new Core.Models.Media(model.Name, model.ParentId, contentType);
        }

        protected override ContentItemDto<IMedia> MapFromPersisted(MediaItemSave model)
        {
            return Mapper.Map<IMedia, ContentItemDto<IMedia>>(model.PersistedContent);
        }
    }
}