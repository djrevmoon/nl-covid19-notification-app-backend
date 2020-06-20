// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Icc.Exceptions;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Icc.Models;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ICC.Models;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ICC.Services;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Services;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Icc.Services
{
    public class IccService : IIccService
    {
        private readonly IccBackendContentDbContext _DbContext;
        private readonly IConfiguration _Configuration;
        private readonly IUtcDateTimeProvider _DateTimeProvider;

        public IccService(IccBackendContentDbContext dbContext, IConfiguration configuration,
            IUtcDateTimeProvider dateTimeProvider)
        {
            _DbContext = dbContext;
            _Configuration = configuration;
            _DateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// Get InfectionConfirmationCodeEntity by raw icc string
        /// </summary>
        /// <param name="icc"></param>
        /// <returns></returns>
        /// <exception cref="IccNotFoundException"></exception>
        public async Task<InfectionConfirmationCodeEntity> Get(string icc)
        {
            var infectionConfirmationCodeEntity = await _DbContext.InfectionConfirmationCodes.FindAsync(icc);
            if (infectionConfirmationCodeEntity == null) throw new IccNotFoundException();
            return infectionConfirmationCodeEntity;
        }

        /// <summary>
        /// Checks if Icc exists and is valid
        /// </summary>
        /// <param name="iccCodeString"></param>
        /// <returns>Icc if valid else null</returns>
        public async Task<InfectionConfirmationCodeEntity> Validate(string iccCodeString)
        {
            var infectionConfirmationCodeEntity = await Get(iccCodeString);
            if (infectionConfirmationCodeEntity != null && infectionConfirmationCodeEntity.IsValid())
                return infectionConfirmationCodeEntity;
            return null;
        }


        private Random _Random = new Random();

        private string RandomString(int length, string chars)
        {
            // TODO: Replace with Component version
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_Random.Next(s.Length)]).ToArray());
        }


        /// <summary>
        /// Generate Icc with configuration length and A-Z, 0-9 characters
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="save"></param>
        /// <param name="batchId"></param>
        /// <returns></returns>
        public async Task<InfectionConfirmationCodeEntity> GenerateIcc(Guid userId, string batchId)
        {
            _Random = new Random();
            var length = Convert.ToInt32(_Configuration.GetSection("IccConfig:Code:Length").Value);
            var chars = _Configuration.GetSection("IccConfig:Code:Chars").Value;
            var generatedIcc = RandomString(length, chars);

            var icc = new InfectionConfirmationCodeEntity
            {
                Code = generatedIcc, GeneratedBy = userId, Created = _DateTimeProvider.Now(), BatchId = batchId
            };


            await _DbContext.InfectionConfirmationCodes.AddAsync(icc);
            return icc;
        }

        /// <summary>
        /// Generate Icc batch with size [count]
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IccBatch> GenerateBatch(Guid userId, int count = 20)
        {
            string batchId = RandomString(6, "ABCDEFGHIJKLMNOP0123");
            IccBatch batch = new IccBatch(batchId);

            for (var i = 0; i < count; i++)
            {
                batch.AddIcc(await GenerateIcc(userId, batchId));
            }
            
            return batch;
        }

        public async Task<InfectionConfirmationCodeEntity> RedeemIcc(string icc)
        {
            var infectionConfirmationCodeEntity = await Get(icc);
            infectionConfirmationCodeEntity.Used = _DateTimeProvider.Now();

            return infectionConfirmationCodeEntity;
        }

        public async Task<bool> RevokeBatch(RevokeBatchInput revokeBatchInput)
        {
            List<InfectionConfirmationCodeEntity> iccList =
                await _DbContext.InfectionConfirmationCodes.Where(i => i.BatchId == revokeBatchInput.BatchId)
                    .ToListAsync();

            if (iccList.Count > 0)
            {
                foreach (InfectionConfirmationCodeEntity infectionConfirmationCodeEntity in iccList)
                {
                    infectionConfirmationCodeEntity.Revoked =
                        revokeBatchInput.RevokeDateTime ?? _DateTimeProvider.Now();
                }

                return true;
            }

            return false;
        }
    }
}