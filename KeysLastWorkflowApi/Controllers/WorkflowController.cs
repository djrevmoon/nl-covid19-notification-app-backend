﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.WebApi;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow.RegisterSecret;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow.SendTeks;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.WorkflowApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkflowController : ControllerBase
    {
        [HttpPost]
        [Route(EndPointNames.MobileAppApi.ReleaseTeks)]
        public async Task<IActionResult> PostWorkflow([FromQuery] byte[] sig, [FromServices] HttpPostReleaseTeksCommand command)
        {
            await command.Execute(sig, Request);
            return Ok(new{});
        }

        [HttpPost]
        [Route(EndPointNames.MobileAppApi.RegisterSecret)]
        public async Task<IActionResult> PostSecret([FromBody]SecretArgs _, [FromServices]HttpPostRegisterSecret command)
        {
            return await command.Execute();
        }
    }
}
