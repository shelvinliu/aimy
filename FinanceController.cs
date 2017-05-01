  [HttpGet]
        public JsonResult GetPaymentHistory(string status, DateTime? dateFrom, DateTime? dateTo, int? parentId, int? paymentId, string paymentReference, string invoiceCode)
        {
            using (var db = SiteUtil.NewDb)
            {
                try
                {
                    //var invoiceId = invoiceCode.Split('-').Select(x => Int32.TryParse(x)).FirstOrDefault();
                    int invoiceId = invoiceCode.Replace("INV-", "").ParseToInt();
                    var orgSetting = db.OrgSettings.Where(x => x.OrgId == SiteUtil.CurrentOrg.Id).FirstOrDefault();
                    IList<int> paymentIds = new List<int>();
                    if (invoiceCode != null)
                    {
                        if (orgSetting != null)
                        {
                            var paymentBatch = db.Payments.Where(x => x.IsActive && (x.Invoice.XeroInvoiceCode == invoiceCode || x.XeroInvoiceId == invoiceId) && x.PaymentBatch.IsActive)
                            .Select(x => x.PaymentBatch).ToList();
                            if (paymentBatch != null)
                            {
                                paymentIds = db.Payments.Where(x => x.PaymentBatchId == paymentBatch.Select(s=>s.Id).FirstOrDefault()).Select(x => x.Id).ToList();
                            }
                        }
                        else
                        {
                            var paymentBatch = db.Payments.Where(x => x.IsActive && x.InvoiceId == invoiceId && x.PaymentBatch.IsActive)
                            .Select(x => x.PaymentBatch).FirstOrDefault();
                            if (paymentBatch != null)
                            {
                                paymentIds = db.Payments.Where(x => x.PaymentBatchId == paymentBatch.Id).Select(x => x.Id).ToList();
                            }
                        }

                    }
                    var isInvoiceManagerSite = orgSetting == null;

                    var paymentList = db.Payments.
                                      Where(x => x.SiteId == SiteUtil.CurrentOrg.Id &&
                                                 x.PaymentBatch.PaymentReference.Contains(paymentReference) &&
                                                 x.IsActive &&

                                                (status == "Show all" || x.PaymentBatch.Status == status) &&
                                                (parentId == null || x.Billing.UserId == parentId) &&
                                                (paymentId == null || x.PaymentBatch.Id == paymentId) &&
                                                (invoiceCode == "" || paymentIds.Contains(x.Id)) &&
                                                ((x.PaymentBatch.PaidOn >= dateFrom && x.PaymentBatch.PaidOn <= dateTo)
                                                  || (dateFrom == null && dateTo == null))).
                                      Select(c => new
                                      {
                                          XeroInvoiceCode = isInvoiceManagerSite ? "INV-" + c.InvoiceId: c.Invoice.XeroInvoiceCode ?? "INV-" + c.XeroInvoiceId,//?? c.InvoiceId),
                                          PaidAmount = c.PaidAmount,
                                          PaidOn = c.PaymentBatch.PaidOn,
                                          LocalizePaidOn = "",
                                          PaymentType = c.PaymentBatch.PaymentType,
                                          PaymentReference = c.PaymentBatch.PaymentReference,
                                          Status = c.PaymentBatch.Status,
                                          ContactName = c.Billing.User.Contact.FirstName + " " +
                                                        c.Billing.User.Contact.LastName,
                                      }).ToList();

                    paymentList = (from a in paymentList
                                   select new
                                   {
                                       XeroInvoiceCode = a.XeroInvoiceCode,
                                       PaidAmount = a.PaidAmount,
                                       PaidOn = a.PaidOn,
                                       LocalizePaidOn = a.PaidOn.FormatToShortDate(),
                                       PaymentType = a.PaymentType,
                                       PaymentReference = a.PaymentReference,
                                       Status = a.Status,
                                       ContactName = a.ContactName,

                                   }).ToList();


                    return Json(paymentList, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                    Logging.Error(e.Message, e);

                    return Json(new { Success = false, Message = e.ToString() });
                }
            }
        }

        [HttpGet]
        public JsonResult GetPaidInvoices()
        {
            using (var db = SiteUtil.NewDb)
            {
                try
                {
                    var orgSetting = db.OrgSettings.Where(x=>x.Org.Id==SiteUtil.CurrentOrg.Id).FirstOrDefault();
                    var isInvoiceManagerSite = orgSetting == null;
                    var invoices = db.Payments.Where(x =>x.SiteId==SiteUtil.CurrentOrg.Id && x.IsActive && x.PaymentBatch.IsActive)
                        .Select(x => new
                        {
                            Id = x.InvoiceId,
                            InvoiceNum = isInvoiceManagerSite ? "INV-" + x.InvoiceId : x.Invoice.XeroInvoiceCode ?? ("INV-" + x.XeroInvoiceId),
                        }).DistinctBy(x => x.InvoiceNum).ToList();
                    return Json(invoices, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                    Logging.Error(e.Message, e);

                    return Json(new { Success = false, Message = e.ToString() });
                }
            }
        }