﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sushi.Mediakiwi.API.Filters;
using Sushi.Mediakiwi.API.Services;
using Sushi.Mediakiwi.API.Transport.Requests;
using Sushi.Mediakiwi.API.Transport.Responses;
using System.Threading.Tasks;

namespace Sushi.Mediakiwi.API.Controllers
{
    [ApiController]
    [MediakiwiApiAuthorize]
    [Route(Common.MK_CONTROLLERS_PREFIX + "content")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class Content : BaseMediakiwiApiController
    {
        private readonly IContentService _contentService;

        public Content(IContentService _service)
        {
            _contentService = _service;
        }

        /// <summary>
        /// Returns the CMS content belonging to the URL being viewed.
        /// This can be anything from a Page, List, Gallery, Asset or a Folder Explorer
        /// </summary>
        /// <param name="request">The request containing the needed information</param>
        /// <returns></returns>
        /// <response code="200">The Content is succesfully retrieved</response>
        /// <response code="400">Some needed information is missing from the request</response>
        /// <response code="401">The user is not succesfully authenticated</response>
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("GetContent")]
        public async Task<ActionResult<GetContentResponse>> GetContent([FromBody] GetContentRequest request)
        {
            GetContentResponse result = new GetContentResponse();

            if (request.CurrentSiteID == 0)
            {
                return BadRequest();
            }

            // We are looking at a list, but not the Browsing list
            if (Resolver.ListInstance != null && Resolver.List.ClassName.Contains("Sushi.Mediakiwi.AppCentre.Data.Implementation.Browsing", System.StringComparison.InvariantCultureIgnoreCase)== false)
            {
                result.List = await _contentService.GetListResponseAsync(Resolver).ConfigureAwait(false);
                result.IsEditMode = result.List.IsEditMode;
                result.StatusCode = System.Net.HttpStatusCode.OK;
            }
            // We are looking at a page
            else if (Resolver.Page != null)
            {
                //result.Page = await _contentService.GetExplorerResponseAsync(Resolver).ConfigureAwait(false);
            }
            // We are browsing
            else 
            {
                result.Explorer = await _contentService.GetExplorerResponseAsync(Resolver).ConfigureAwait(false);
                result.StatusCode = System.Net.HttpStatusCode.OK;
            }

            return Ok(result);
        }

        /// <summary>
        /// Handles the incoming Post parameters and returns the CMS content belonging to the URL being viewed.
        /// This can be anything from a Page, List, Gallery, Asset or a Folder Explorer
        /// </summary>
        /// <param name="request">The request containing the needed information</param>
        /// <returns></returns>
        /// <response code="200">The Content is succesfully retrieved</response>
        /// <response code="400">Some needed information is missing from the request</response>
        /// <response code="401">The user is not succesfully authenticated</response>
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("PostContent")]
        public async Task<ActionResult<PostContentResponse>> PostContent([FromBody] PostContentRequest request)
        {
            PostContentResponse result = new PostContentResponse();

            if (request.CurrentSiteID == 0 || string.IsNullOrEmpty(request.PostedField))
            {
                return BadRequest();
            }

            // We are looking at a list
            if (Resolver.ListInstance != null)
            {
                result.List = await _contentService.GetListResponseAsync(Resolver).ConfigureAwait(false);
                result.IsEditMode = result.List.IsEditMode;
                result.StatusCode = System.Net.HttpStatusCode.OK;
            }

            return Ok(result);
        }
    }
}