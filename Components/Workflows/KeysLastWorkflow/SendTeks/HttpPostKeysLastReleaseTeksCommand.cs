﻿// Copyright © 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using Microsoft.AspNetCore.Mvc;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.RiskCalculationConfig;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflows.KeysFirstWorkflow.EscrowTeks;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflows.KeysLastWorkflow.SendTeks
{
    public class HttpPostKeysLastReleaseTeksCommand
    {
        private readonly IKeysLastWorkflowValidator _Validator;
        private readonly IKeysLastTekWriter _Writer;
        private readonly IDbContextProvider<ExposureContentDbContext> _DbContextProvider;

        public HttpPostKeysLastReleaseTeksCommand(IKeysLastWorkflowValidator validator, IKeysLastTekWriter writer, DbContextProvider<ExposureContentDbContext> dbContextProvider)
        {
            _Validator = validator;
            _Writer = writer;
            _DbContextProvider = dbContextProvider;
        }

        public IActionResult Execute(KeysLastReleaseTeksArgs args)
        {
            if (!_Validator.Validate(args))
            {
                //TODO log bad request
                return new OkResult();
            }

            _DbContextProvider.BeginTransaction();
            _Writer.Execute(args);
            _DbContextProvider.SaveAndCommit();
            return new OkResult();
        }
    }
}