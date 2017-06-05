using Microsoft.AspNetCore.Mvc.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using Swashbuckle.AspNetCore.Swagger;

namespace PaderbornUniversity.SILab.Hip.Webservice
{
	/// <summary>
	/// Swagger operation filter. It inspects the ApiDescription for every action for
	/// relevant information (in this case, whether the user is already authorized
	/// and whether anonymous access is allowed) and then updates the Swagger 
	/// Operation by adding an Authorization field in which the user can input
	/// an access token.
	/// </summary>
	public class SwaggerOperationFilter : IOperationFilter
	{
		public void Apply(Operation operation, OperationFilterContext context)
		{
			var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
			var isAuthorized = filterPipeline.Select(filterInfo => filterInfo.Filter).Any(filter => filter is AuthorizeFilter);
			var allowAnonymous = filterPipeline.Select(filterInfo => filterInfo.Filter).Any(filter => filter is IAllowAnonymousFilter);

			if (!isAuthorized || allowAnonymous)
				return;
			if (operation.Parameters == null)
				operation.Parameters = new List<IParameter>();

			operation.Parameters.Add(new NonBodyParameter
			{
				Name = "Authorization",
				In = "header",
				Description = "access token",
				Required = true,
				Type = "string"
			});
			operation.Responses.Add("401", new Response { Description = "Unauthorized" });
		}
	}
}
