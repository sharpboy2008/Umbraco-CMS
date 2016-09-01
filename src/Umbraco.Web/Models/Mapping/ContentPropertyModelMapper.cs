﻿using System;
using AutoMapper;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Mapping;
using Umbraco.Core.Services;
using Umbraco.Web.Models.ContentEditing;

namespace Umbraco.Web.Models.Mapping
{
    /// <summary>
    /// A mapper which declares how to map content properties. These mappings are shared among media (and probably members) which is 
    /// why they are in their own mapper
    /// </summary>
    internal class ContentPropertyModelMapper : ModelMapperConfiguration
    {
        private readonly IDataTypeService _dataTypeService;

        public ContentPropertyModelMapper(IDataTypeService dataTypeService)
        {
            _dataTypeService = dataTypeService;
        }

        public override void ConfigureMappings(IMapperConfiguration config)
        {
            var lazyDataTypeService = new Lazy<IDataTypeService>(() => _dataTypeService);

            //FROM Property TO ContentPropertyBasic
            config.CreateMap<PropertyGroup, Tab<ContentPropertyDisplay>>()
                .ForMember(tab => tab.Label, expression => expression.MapFrom(@group => @group.Name))
                .ForMember(tab => tab.IsActive, expression => expression.UseValue(true))
                .ForMember(tab => tab.Properties, expression => expression.Ignore())
                .ForMember(tab => tab.Alias, expression => expression.Ignore());

            //FROM Property TO ContentPropertyBasic
            config.CreateMap<Property, ContentPropertyBasic>()
                  .ConvertUsing(new ContentPropertyBasicConverter<ContentPropertyBasic>(lazyDataTypeService));

            //FROM Property TO ContentPropertyDto
            config.CreateMap<Property, ContentPropertyDto>()
                  .ConvertUsing(new ContentPropertyDtoConverter(lazyDataTypeService));

            //FROM Property TO ContentPropertyDisplay
            config.CreateMap<Property, ContentPropertyDisplay>()
                  .ConvertUsing(new ContentPropertyDisplayConverter(lazyDataTypeService));
        }
    }
}