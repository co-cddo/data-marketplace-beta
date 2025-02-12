using System.Text.RegularExpressions;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.Answers;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.OptionSelectionItems;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;
using Cddo.Data.Marketplace.UI.Model.Countries;
using Cddo.Data.Marketplace.UI.Pages.DataShare;
using Cddo.Data.Marketplace.UI.Pages.DataShare._Partial;
using Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats;

namespace Cddo.Data.Marketplace.UI.Builders;

internal class QuestionDataBuilder(ILogger<QuestionDataBuilder> logger,
    ICountrySelectionPresenter countrySelectionPresenter) : IQuestionDataBuilder
{
    #region BuildQuestionModelFromDataShareRequestQuestion
    QuestionModel IQuestionDataBuilder.BuildQuestionModelFromDataShareRequestQuestion(
        DataShareRequestQuestion dataShareRequestQuestion)
    {
        ArgumentNullException.ThrowIfNull(dataShareRequestQuestion);

        return new QuestionModel
        {
            DataShareRequestId = dataShareRequestQuestion.DataShareRequestId,
            DataShareRequestRequestId = dataShareRequestQuestion.DataShareRequestRequestId,
            QuestionId = dataShareRequestQuestion.QuestionId,
            Footer = dataShareRequestQuestion.QuestionFooter,
            QuestionParts = [.. BuildQuestionParts(dataShareRequestQuestion.IsOptional, dataShareRequestQuestion.QuestionParts)]
        };
    }

    private IEnumerable<QuestionPartModel> BuildQuestionParts(
        bool questionIsOptional,
        IEnumerable<DataShareRequestQuestionPart> questionParts)
    {
        foreach (var questionPart in questionParts.OrderBy(x => x.QuestionPartQuestion.QuestionPartOrderWithinQuestion))
        {
            var questionPartQuestion = questionPart.QuestionPartQuestion;
            var questionPartResponses = BuildQuestionPartResponses(
                    questionPartQuestion.Id,
                    questionPartQuestion.QuestionPartOrderWithinQuestion,
                    questionPartQuestion.ResponseFormat,
                    questionPartQuestion.MultipleAnswerItemControl,
                    questionPart.QuestionPartAnswer);

            yield return new QuestionPartModel
            {
                QuestionPartId = questionPartQuestion.Id,
                ResponseFormat = questionPartQuestion.ResponseFormat.FormatType,
                OrderWithinQuestion = questionPartQuestion.QuestionPartOrderWithinQuestion,
                QuestionText = questionPartQuestion.Prompts.QuestionText,
                HintText = questionPartQuestion.Prompts.HintText,
                QuestionIsOptional = questionIsOptional,
                MultipleResponsesAreAllowed = questionPartQuestion.MultipleAnswerItemControl.MultipleAnswerItemsAreAllowed,
                ItemDescriptionIfMultipleResponsesAreAllowed = questionPartQuestion.MultipleAnswerItemControl.ItemDescription,
                QuestionPartResponses = [.. questionPartResponses]

            };
        }
    }

    private IEnumerable<QuestionPartResponseModel> BuildQuestionPartResponses(
        Guid questionPartId,
        int questionPartNumber,
        QuestionPartResponseFormatBase questionPartResponseFormat,
        QuestionPartMultipleAnswerItemControl multipleAnswerItemControl,
        QuestionPartAnswer? questionPartAnswer)
    {
        if (questionPartAnswer != null && questionPartAnswer.QuestionPartId != questionPartId)
        {
            logger.LogError("Answer part received for mismatching question part");
        }

        var responses = questionPartAnswer?.AnswerPartResponses ?? [];

        var maximumResponseLength = DetermineMaximumResponseLength(questionPartResponseFormat);

        // If there are no responses yet, then add an empty one here, so that the following code can flow
        // without having 'if answered' etc
        if (!responses.Any())
        {
            responses.Add(new QuestionPartAnswerResponse
            {
                InputType = questionPartResponseFormat.InputType,
                OrderWithinAnswerPart = 1,
                ResponseItem = BuildEmptyResponseItem()
            });

            QuestionPartAnswerResponseItemBase? BuildEmptyResponseItem()
            {
                switch (questionPartResponseFormat.InputType)
                {
                    case QuestionPartResponseInputType.FreeForm:
                        {
                            return new QuestionPartAnswerResponseItemFreeForm
                            {
                                EnteredValue = "",
                                ValueEntryDeclined = false,
                                MaximumResponseLength = maximumResponseLength
                            };
                        }

                    case QuestionPartResponseInputType.OptionSelection:
                        return new QuestionPartAnswerResponseItemSelectionOption
                        {
                            SelectedOptions = []
                        };

                    case QuestionPartResponseInputType.None:
                        return null;

                    default:
                        logger.LogError("Unable to build empty response item for unknown input type");
                        return null;
                }
            }
        }

        foreach (var response in responses)
        {
            var responseItem = BuildResponseItem(
                    response,
                    response.ValidationErrors.Any());

            yield return new QuestionPartResponseModel
            {
                QuestionPartId = questionPartId,
                ResponseNumber = response.OrderWithinAnswerPart,
                ResponseFormat = questionPartResponseFormat.FormatType,
                MultipleResponsesAreAllowed = multipleAnswerItemControl.MultipleAnswerItemsAreAllowed,
                ResponseItemDescriptionIfMultipleResponsesAreAllowed = multipleAnswerItemControl.ItemDescription,
                AttachRemoveButton = responses.Count > 1,
                ValidationErrors = response.ValidationErrors,
                ResponseItem = responseItem,
                MaxResponseLength = maximumResponseLength,
                inputAreaID = responseItem.inputAreaID
            };

        }

        QuestionPartResponseItemModel BuildResponseItem(
            QuestionPartAnswerResponse response,
            bool responseIsInvalid)
        {
            return questionPartResponseFormat.FormatType switch
            {
                QuestionPartResponseFormatType.Text => BuildResponseItemFreeFormText(
                    questionPartId, questionPartNumber, (QuestionPartResponseFormatFreeFormText)questionPartResponseFormat, response, responseIsInvalid, multipleAnswerItemControl),

                QuestionPartResponseFormatType.Date => BuildResponseItemFreeFormDate(
                    questionPartId, questionPartNumber, responseIsInvalid, response),

                QuestionPartResponseFormatType.Country => BuildResponseItemFreeFormCountry(
                    questionPartId, questionPartNumber, responseIsInvalid, response),

                QuestionPartResponseFormatType.ReadOnly => BuildResponseItemReadOnly(
                    questionPartId, questionPartNumber, responseIsInvalid, response),

                QuestionPartResponseFormatType.SelectSingle => BuildResponseOptionSelectionSingleValue(
                    (QuestionPartResponseFormatOptionSelectSingleValue)questionPartResponseFormat, questionPartId, questionPartNumber, responseIsInvalid, response),

                QuestionPartResponseFormatType.SelectMulti => BuildResponseItemOptionSelectionMultiValue(
                    (QuestionPartResponseFormatOptionSelectMultiValue)questionPartResponseFormat, questionPartId, questionPartNumber, responseIsInvalid, response),

                _ => throw new Exception("Response format type has no associated display page")
            };
        }
    }

    private static int DetermineMaximumResponseLength(QuestionPartResponseFormatBase? questionPartResponseFormat)
    {
        const int defaultMaximumResponseLength = int.MaxValue;

        switch (questionPartResponseFormat?.FormatType)
        {
            case QuestionPartResponseFormatType.Text:
                {
                    var questionPartResponseFormatFreeFormText = (QuestionPartResponseFormatFreeFormText)questionPartResponseFormat;
                    return questionPartResponseFormatFreeFormText.MaximumResponseLength;
                }

            case QuestionPartResponseFormatType.SelectMulti:
                {
                    var responseFormatOptionSelectMultiValue = (QuestionPartResponseFormatOptionSelectMultiValue)questionPartResponseFormat;

                    var firstSupplementaryQuestion = responseFormatOptionSelectMultiValue.MultiSelectionOptions?.Select(x => x.SupplementaryQuestion)
                        .FirstOrDefault(x => x?.ResponseFormat.FormatType == QuestionPartResponseFormatType.Text);

                    if (firstSupplementaryQuestion == null) return defaultMaximumResponseLength;

                    return ((QuestionPartResponseFormatFreeFormText)firstSupplementaryQuestion.ResponseFormat).MaximumResponseLength;
                }

            case QuestionPartResponseFormatType.SelectSingle:
                {
                    var responseFormatOptionSelectSingleValue = (QuestionPartResponseFormatOptionSelectSingleValue)questionPartResponseFormat;

                    var firstSupplementaryQuestion = responseFormatOptionSelectSingleValue.SingleSelectionOptions?.Select(x => x.SupplementaryQuestion)
                        .FirstOrDefault(x => x?.ResponseFormat.FormatType == QuestionPartResponseFormatType.Text);

                    if (firstSupplementaryQuestion == null) return defaultMaximumResponseLength;

                    return ((QuestionPartResponseFormatFreeFormText)firstSupplementaryQuestion.ResponseFormat).MaximumResponseLength;
                }

            default:
                return defaultMaximumResponseLength;
        }
    }

    private static QuestionPartResponseItemFreeFormText BuildResponseItemFreeFormText(
        Guid questionPartId,
        int questionPartNumber,
        QuestionPartResponseFormatFreeFormText questionPartFormatFreeFormText,
        QuestionPartAnswerResponse response,
        bool responseIsInvalid,
        QuestionPartMultipleAnswerItemControl multipleAnswerItemControl)
    {
        var responseItemFreeForm = (QuestionPartAnswerResponseItemFreeForm)response.ResponseItem!;

        var responseNumber = response.OrderWithinAnswerPart;

        var inputComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-text-value";
        var inputComponentName = $"questionPart{questionPartNumber}Response{responseNumber}TextResponse";

        return new QuestionPartResponseItemFreeFormText
        {
            TextInputComponentId = inputComponentId,
            TextInputComponentName = inputComponentName,
            QuestionPartId = questionPartId,
            QuestionPartNumber = questionPartNumber,
            ResponseNumber = responseNumber,
            ResponseIsInvalid = responseIsInvalid,
            IsShortAnswer = multipleAnswerItemControl.MultipleAnswerItemsAreAllowed,
            EnteredValue = responseItemFreeForm.EnteredValue,
            MaximumResponseLength = questionPartFormatFreeFormText.MaximumResponseLength,
            inputAreaID = inputComponentId
        };
    }

    private static QuestionPartResponseItemFreeFormDate BuildResponseItemFreeFormDate(
        Guid questionPartId,
        int questionPartNumber,
        bool responseIsInvalid,
        QuestionPartAnswerResponse response)
    {
        var responseItemFreeForm = (QuestionPartAnswerResponseItemFreeForm)response.ResponseItem!;

        var responseNumber = response.OrderWithinAnswerPart;

        var dayInputComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-day-value";
        var monthInputComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-month-value";
        var yearInputComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-year-value";

        var dayInputComponentName = $"questionPart{questionPartNumber}Response{responseNumber}DayResponse";
        var monthInputComponentName = $"questionPart{questionPartNumber}Response{responseNumber}MonthResponse";
        var yearInputComponentName = $"questionPart{questionPartNumber}Response{responseNumber}YearResponse";

        var dateHasBeenEntered = responseItemFreeForm.EnteredValue.Length == 8;
        var yearPart = dateHasBeenEntered ? responseItemFreeForm.EnteredValue.Substring(0, 4) : "";
        var monthPart = dateHasBeenEntered ? responseItemFreeForm.EnteredValue.Substring(4, 2) : "";
        var dayPart = dateHasBeenEntered ? responseItemFreeForm.EnteredValue.Substring(6, 2) : "";

        return new QuestionPartResponseItemFreeFormDate
        {
            DayInputComponentId = dayInputComponentId,
            MonthInputComponentId = monthInputComponentId,
            YearInputComponentId = yearInputComponentId,

            DayInputComponentName = dayInputComponentName,
            MonthInputComponentName = monthInputComponentName,
            YearInputComponentName = yearInputComponentName,

            QuestionPartId = questionPartId,
            QuestionPartNumber = questionPartNumber,
            ResponseNumber = responseNumber,

            EnteredDayPart = dayPart,
            EnteredMonthPart = monthPart,
            EnteredYearPart = yearPart,

            ResponseIsInvalid = responseIsInvalid
        };
    }

    private QuestionPartResponseItemFreeFormCountry BuildResponseItemFreeFormCountry(
        Guid questionPartId,
        int questionPartNumber,
        bool responseIsInvalid,
        QuestionPartAnswerResponse response)
    {
        var responseItemFreeForm = (QuestionPartAnswerResponseItemFreeForm)response.ResponseItem!;

        var responseNumber = response.OrderWithinAnswerPart;

        var inputComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-country-value";
        var inputComponentName = $"questionPart{questionPartNumber}Response{responseNumber}CountryResponse";

        var selectableCountries = countrySelectionPresenter.CountrySelectionsWithoutUnitedKingdom.ToList();

        // Country entry is a facility to replace a previous current plain text entry
        // method.  So if a value was previously entered with a plain text then we add
        // an entry for that
        var enteredValue = responseItemFreeForm.EnteredValue;

        if (!string.IsNullOrEmpty(enteredValue) &&
            selectableCountries.All(x => x.CountryName != enteredValue))
        {
            selectableCountries.Add(new CountrySelection
            {
                Id = enteredValue,
                CountryName = enteredValue
            });
        }

        return new QuestionPartResponseItemFreeFormCountry
        {
            CountryInputComponentId = inputComponentId,
            CountryInputComponentName = inputComponentName,
            QuestionPartId = questionPartId,
            QuestionPartNumber = questionPartNumber,
            ResponseNumber = responseNumber,
            ResponseIsInvalid = responseIsInvalid,
            EnteredValue = enteredValue,
            SelectableCountries = selectableCountries,
            inputAreaID = inputComponentId
        };
    }

    private static QuestionPartResponseItemReadOnly BuildResponseItemReadOnly(
        Guid questionPartId,
        int questionPartNumber,
        bool responseIsInvalid,
        QuestionPartAnswerResponse response)
    {
        var responseNumber = response.OrderWithinAnswerPart;

        return new QuestionPartResponseItemReadOnly
        {
            QuestionPartId = questionPartId,
            QuestionPartNumber = questionPartNumber,
            ResponseNumber = responseNumber,
            ResponseIsInvalid = responseIsInvalid
        };
    }

    private static QuestionPartResponseItemOptionSelectionSingleValue BuildResponseOptionSelectionSingleValue(
        QuestionPartResponseFormatOptionSelectSingleValue questionPartResponseFormatOptionSelectSingleValue,
        Guid questionPartId,
        int questionPartNumber,
        bool responseIsInvalid,
        QuestionPartAnswerResponse response)
    {
        var responseNumber = response.OrderWithinAnswerPart;

        return new QuestionPartResponseItemOptionSelectionSingleValue
        {
            QuestionPartId = questionPartId,
            QuestionPartNumber = questionPartNumber,
            ResponseNumber = responseNumber,
            ResponseIsInvalid = responseIsInvalid,
            SelectionOptions = questionPartResponseFormatOptionSelectSingleValue.SingleSelectionOptions
                .Select(BuildSingleValueSelectionOption).ToList()
        };

        SelectionOptionInSingleValueSetModel BuildSingleValueSelectionOption(
            QuestionPartOptionSelectionItemForSingleSelection selectionItemForSingleSelection)
        {
            var responseItemSelectionOption = (QuestionPartAnswerResponseItemSelectionOption)response.ResponseItem!;

            var selectedOption = responseItemSelectionOption.SelectedOptions.SingleOrDefault(x => x.OptionSelectionItemId == selectionItemForSingleSelection.Id);
            var optionIsSelected = selectedOption != null;
            var supplementaryAnswerValueInResponse = ReadSupplementaryAnswerValueFromResponse();

            var inputComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-option-{selectionItemForSingleSelection.Id}";
            var inputComponentName = $"questionPart{questionPartNumber}Response{responseNumber}OptionsResponse";

            var supplementaryQuestionComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-option-{selectionItemForSingleSelection.Id}-supplementary";
            var supplementaryQuestionComponentName = $"questionPart{questionPartNumber}Response{responseNumber}Option{selectionItemForSingleSelection.Id}Supplementary";

            var supplementaryQuestionPartIdComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-option-{selectionItemForSingleSelection.Id}-supplementary-question-part-id";
            var supplementaryQuestionPartIdComponentName = $"questionPart{questionPartNumber}Response{responseNumber}Option{selectionItemForSingleSelection.Id}SupplementaryQuestionPartId";

            return new SelectionOptionInSingleValueSetModel
            {
                SelectionOptionInputComponentId = inputComponentId,
                SelectionOptionInputComponentName = inputComponentName,
                SelectionOptionId = selectionItemForSingleSelection.Id,
                OptionOrderWithinSet = selectionItemForSingleSelection.OptionOrderWithinSelection,
                ValueText = selectionItemForSingleSelection.ValueText ?? "",
                HintText = selectionItemForSingleSelection.HintText,

                IsAlternativeAnswer = selectionItemForSingleSelection.IsAlternativeAnswer,
                IsSelected = optionIsSelected,
                SupplementaryQuestionInputComponentId = supplementaryQuestionComponentId,
                SupplementaryQuestionInputComponentName = supplementaryQuestionComponentName,
                SupplementaryQuestionPartId = selectionItemForSingleSelection.SupplementaryQuestion?.Id,
                SupplementaryQuestionText = selectionItemForSingleSelection.SupplementaryQuestion?.Prompts.QuestionText,
                SupplementaryQuestionPartIdComponentId = supplementaryQuestionPartIdComponentId,
                SupplementaryQuestionPartIdComponentName = supplementaryQuestionPartIdComponentName,
                SupplementaryQuestionEnteredValue = supplementaryAnswerValueInResponse,
                SupplementaryQuestionMaximumResponseLength = DetermineMaximumResponseLength(selectionItemForSingleSelection.SupplementaryQuestion?.ResponseFormat)
            };

            string? ReadSupplementaryAnswerValueFromResponse()
            {
                var supplementaryResponse = selectedOption?.SupplementaryQuestionPartAnswer?.AnswerPartResponses.FirstOrDefault();
                if (supplementaryResponse == null) return null;

                var supplementaryResponseItem = supplementaryResponse.ResponseItem;
                if (supplementaryResponseItem == null) return null;

                var supplementaryResponseItemFreeForm = (QuestionPartAnswerResponseItemFreeForm)supplementaryResponseItem;
                return supplementaryResponseItemFreeForm.EnteredValue;
            }
        }
    }

    private static QuestionPartResponseItemOptionSelectionMultiValue BuildResponseItemOptionSelectionMultiValue(
        QuestionPartResponseFormatOptionSelectMultiValue questionPartResponseFormatOptionSelectMultiValue,
        Guid questionPartId,
        int questionPartNumber,
        bool responseIsInvalid,
        QuestionPartAnswerResponse response)
    {
        var responseNumber = response.OrderWithinAnswerPart;

        return new QuestionPartResponseItemOptionSelectionMultiValue
        {
            QuestionPartId = questionPartId,
            QuestionPartNumber = questionPartNumber,
            ResponseNumber = responseNumber,
            ResponseIsInvalid = responseIsInvalid,
            SelectionOptions = questionPartResponseFormatOptionSelectMultiValue.MultiSelectionOptions?
                .Select(BuildMultiValueSelectionOption).ToList() ?? []
        };

        SelectionOptionInMultiValueSetModel BuildMultiValueSelectionOption(
            QuestionPartOptionSelectionItemForMultiSelection selectionItemForMultiSelection)
        {
            var responseItemSelectionOption = (QuestionPartAnswerResponseItemSelectionOption)response.ResponseItem!;

            var selectedOption = responseItemSelectionOption.SelectedOptions.SingleOrDefault(x => x.OptionSelectionItemId == selectionItemForMultiSelection.Id);
            var optionIsSelected = selectedOption != null;
            var supplementaryAnswerValueInResponse = ReadSupplementaryAnswerValueFromResponse();

            var inputComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-option-{selectionItemForMultiSelection.Id}";
            var inputComponentName = $"questionPart{questionPartNumber}Response{responseNumber}OptionsResponse";

            var supplementaryQuestionComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-option-{selectionItemForMultiSelection.Id}-supplementary";
            var supplementaryQuestionComponentName = $"questionPart{questionPartNumber}Response{responseNumber}Option{selectionItemForMultiSelection.Id}Supplementary";

            var supplementaryQuestionPartIdComponentId = $"question-part-{questionPartNumber}-response-{responseNumber}-option-{selectionItemForMultiSelection.Id}-supplementary-question-part-id";
            var supplementaryQuestionPartIdComponentName = $"questionPart{questionPartNumber}Response{responseNumber}Option{selectionItemForMultiSelection.Id}SupplementaryQuestionPartId";

            return new SelectionOptionInMultiValueSetModel
            {
                SelectionOptionInputComponentId = inputComponentId,
                SelectionOptionInputComponentName = inputComponentName,
                SelectionOptionId = selectionItemForMultiSelection.Id,
                OptionOrderWithinSet = selectionItemForMultiSelection.OptionOrderWithinSelection,
                ValueText = selectionItemForMultiSelection.ValueText ?? "",
                HintText = selectionItemForMultiSelection.HintText,
                IsMaster = selectionItemForMultiSelection.IsMaster,
                IsSelected = optionIsSelected,

                SupplementaryQuestionInputComponentId = supplementaryQuestionComponentId,
                SupplementaryQuestionInputComponentName = supplementaryQuestionComponentName,

                SupplementaryQuestionPartId = selectionItemForMultiSelection.SupplementaryQuestion?.Id,
                SupplementaryQuestionText = selectionItemForMultiSelection.SupplementaryQuestion?.Prompts.QuestionText,
                SupplementaryQuestionPartIdComponentId = supplementaryQuestionPartIdComponentId,
                SupplementaryQuestionPartIdComponentName = supplementaryQuestionPartIdComponentName,
                SupplementaryQuestionEnteredValue = supplementaryAnswerValueInResponse,
                SupplementaryQuestionMaximumResponseLength = DetermineMaximumResponseLength(selectionItemForMultiSelection.SupplementaryQuestion?.ResponseFormat)
            };

            string? ReadSupplementaryAnswerValueFromResponse()
            {
                var supplementaryResponse = selectedOption?.SupplementaryQuestionPartAnswer?.AnswerPartResponses.FirstOrDefault();
                if (supplementaryResponse == null) return null;

                var supplementaryResponseItem = supplementaryResponse.ResponseItem;
                if (supplementaryResponseItem == null) return null;

                var supplementaryResponseItemFreeForm = (QuestionPartAnswerResponseItemFreeForm)supplementaryResponseItem;
                return supplementaryResponseItemFreeForm.EnteredValue;
            }
        }
    }
    #endregion

    #region BuildQuestionAnswerFromFormData
    DataShareRequestQuestionAnswer IQuestionDataBuilder.BuildQuestionAnswerFromFormData(IFormCollection form)
    {
        var dataShareRequestId = Guid.Parse(ReadFormValue(form, "dataShareRequestId"));
        var questionId = Guid.Parse(ReadFormValue(form, "questionId"));

        var answerParts = BuildAnswerPartsFromFormData(form).ToList();

        return new DataShareRequestQuestionAnswer
        {
            DataShareRequestId = dataShareRequestId,
            QuestionId = questionId,
            AnswerParts = answerParts
        };
    }

    private IEnumerable<DataShareRequestQuestionAnswerPart> BuildAnswerPartsFromFormData(IFormCollection form)
    {
        var numberOfQuestionParts = int.Parse(ReadFormValue(form, "questionPartCount"));

        foreach (var questionPartOrder in Enumerable.Range(1, numberOfQuestionParts))
        {
            var questionPartId = Guid.Parse(ReadFormValue(form, $"questionPart{questionPartOrder}Id"));

            var answerPartResponses = BuildAnswerPartResponsesFromFormData(form, questionPartOrder).ToList();

            if (!answerPartResponses.Any()) continue;

            yield return new DataShareRequestQuestionAnswerPart
            {
                QuestionPartId = questionPartId,
                AnswerPartResponses = answerPartResponses
            };
        }
    }

    private IEnumerable<DataShareRequestQuestionAnswerPartResponseBase> BuildAnswerPartResponsesFromFormData(
        IFormCollection form,
        int questionPartOrder)
    {
        var questionPartResponseFormat = Enum.Parse<QuestionPartResponseFormatType>(ReadFormValue(form, $"questionPart{questionPartOrder}Format"));

        var questionAllowsMultipleResponses = bool.Parse(ReadFormValue(form, $"questionPart{questionPartOrder}IsMultiResponse"));
        if (!questionAllowsMultipleResponses)
        {
            var numberOfQuestionPartResponses = int.Parse(ReadFormValue(form, $"questionPart{questionPartOrder}ResponseCount"));

            foreach (var questionPartResponseOrder in Enumerable.Range(1, numberOfQuestionPartResponses))
            {
                var response = BuildAnswerPartResponseFromFormData(form, questionPartOrder, questionPartResponseOrder, questionPartResponseFormat, questionAllowsMultipleResponses);
                if (response != null) yield return response;
            }
        }
        else
        {
            var supportedMultiResponseFormats = new List<QuestionPartResponseFormatType>
            {
                QuestionPartResponseFormatType.Text,
                QuestionPartResponseFormatType.Country
            };

            // Multi-response question
            if (!supportedMultiResponseFormats.Contains(questionPartResponseFormat))
                logger.LogError("Multi-responses are not supported for this question type");

            // For multiple response questions, it is possible for the user to press the 'add another' link to create an extra response
            // to those that were retrieved with the question data.  If this happens then the javascript clones the first response, thus
            // creating multiple values for response 1.
            //
            // If this happens then the on-screen order is
            // - First, response 1 as retrieved from the service (unless it has been removed)
            // - Second, responses [2 to N] as retrieved from the service (unless they have been removed)
            // - Third, extra response generated from the javascript, which will identify as 'response 1'

            var regex = new Regex("^questionPart1Response[0-9]+[a-zA-Z]*Response$", RegexOptions.None, TimeSpan.FromMilliseconds(500));
            var allResponseKeys = form.Keys.Where(key => regex.IsMatch(key)).ToList();

            var singleResponseRegex = new Regex("^questionPart1Response1[a-zA-Z]*Response$", RegexOptions.None, TimeSpan.FromMilliseconds(500));
            var response1Key = form.Keys.SingleOrDefault(key => singleResponseRegex.IsMatch(key));


            var response1Values = response1Key != null
                ? ReadFormValues(form, response1Key).ToList()
                : [];

            var otherResponseValues = allResponseKeys.Except([response1Key])
                .Select(key => ReadFormValue(form, key)).ToList();

            var orderedResponseValues = new List<string>();
            if (response1Values.Count() > 0)
            {
                orderedResponseValues.Add(response1Values[0]);
            }
            if (otherResponseValues.Any()) orderedResponseValues.AddRange(otherResponseValues);
            if (response1Values.Count > 1) orderedResponseValues.AddRange(response1Values.Skip(1));

            foreach (var responseValue in orderedResponseValues.Select((value, index) =>
                         new { EnteredValue = value, ResponseNumber = index + 1 }))
            {
                yield return new DataShareRequestQuestionAnswerPartResponseFreeForm
                {
                    OrderWithinAnswerPart = responseValue.ResponseNumber,
                    EnteredValue = responseValue.EnteredValue,
                    ValueEntryDeclined = false,
                    MultipleResponsesAreAllowed = questionAllowsMultipleResponses
                };
            }
        }
    }

    private DataShareRequestQuestionAnswerPartResponseBase? BuildAnswerPartResponseFromFormData(IFormCollection form,
        int questionPartNumber,
        int questionPartResponseOrder,
        QuestionPartResponseFormatType questionPartResponseFormat,
        bool questionAllowsMultipleResponses)
    {
        return questionPartResponseFormat switch
        {
            QuestionPartResponseFormatType.Text => BuildFreeFormTextAnswerPartFromFormData(form, questionPartNumber, questionPartResponseOrder, questionAllowsMultipleResponses),
            QuestionPartResponseFormatType.Date => BuildFreeFormDateAnswerPartFromFormData(form, questionPartNumber, questionPartResponseOrder, questionAllowsMultipleResponses),
            QuestionPartResponseFormatType.SelectSingle => BuildOptionSelectSingleValueAnswerPartFromFormData(form, questionPartNumber, questionPartResponseOrder, questionAllowsMultipleResponses),
            QuestionPartResponseFormatType.SelectMulti => BuildOptionSelectMultiValueAnswerPartFromFormData(form, questionPartNumber, questionPartResponseOrder, questionAllowsMultipleResponses),
            _ => null
        };
    }

    private DataShareRequestQuestionAnswerPartResponseFreeForm BuildFreeFormTextAnswerPartFromFormData(
        IFormCollection form,
        int questionPartNumber,
        int questionPartResponseNumber,
        bool questionAllowsMultipleResponses)
    {
        var enteredValue = ReadFormValue(form, $"questionPart{questionPartNumber}Response{questionPartResponseNumber}TextResponse");

        return new DataShareRequestQuestionAnswerPartResponseFreeForm
        {
            OrderWithinAnswerPart = questionPartResponseNumber,
            EnteredValue = enteredValue,
            ValueEntryDeclined = false,
            MultipleResponsesAreAllowed = questionAllowsMultipleResponses
        };
    }

    private DataShareRequestQuestionAnswerPartResponseFreeForm BuildFreeFormDateAnswerPartFromFormData(
        IFormCollection form,
        int questionPartNumber,
        int questionPartResponseNumber,
        bool questionAllowsMultipleResponses)
    {
        var enteredValue = BuildEnteredValue();

        return new DataShareRequestQuestionAnswerPartResponseFreeForm
        {
            OrderWithinAnswerPart = questionPartResponseNumber,
            EnteredValue = enteredValue,
            ValueEntryDeclined = false,
            MultipleResponsesAreAllowed = questionAllowsMultipleResponses
        };

        string BuildEnteredValue()
        {
            var enteredDayValue = ReadFormValue(form, $"questionPart{questionPartNumber}Response{questionPartResponseNumber}DayResponse") ?? "";
            var enteredMonthValue = ReadFormValue(form, $"questionPart{questionPartNumber}Response{questionPartResponseNumber}MonthResponse") ?? "";
            var enteredYearValue = ReadFormValue(form, $"questionPart{questionPartNumber}Response{questionPartResponseNumber}YearResponse") ?? "";

            var valueHasBeenEntered = !string.IsNullOrWhiteSpace(enteredDayValue)
                                      || !string.IsNullOrWhiteSpace(enteredMonthValue)
                                      || !string.IsNullOrWhiteSpace(enteredYearValue);

            if (!valueHasBeenEntered) return "";

            var dayValue = enteredDayValue.PadLeft(2, '0');
            var monthValue = enteredMonthValue.PadLeft(2, '0');
            var yearValue = enteredYearValue.PadLeft(4, '0');

            return $"{yearValue}{monthValue}{dayValue}";
        }
    }

    private DataShareRequestQuestionAnswerPartResponseSelectionOption BuildOptionSelectSingleValueAnswerPartFromFormData(
        IFormCollection form,
        int questionPartNumber,
        int responseNumber,
        bool questionAllowsMultipleResponses)
    {
        return DoBuildOptionSelectAnswerPartFromFormData(form, questionPartNumber, responseNumber, questionAllowsMultipleResponses);
    }

    private DataShareRequestQuestionAnswerPartResponseSelectionOption BuildOptionSelectMultiValueAnswerPartFromFormData(
        IFormCollection form,
        int questionPartNumber,
        int responseNumber,
        bool questionAllowsMultipleResponses)
    {
        return DoBuildOptionSelectAnswerPartFromFormData(form, questionPartNumber, responseNumber, questionAllowsMultipleResponses);
    }

    private DataShareRequestQuestionAnswerPartResponseSelectionOption DoBuildOptionSelectAnswerPartFromFormData(
        IFormCollection form,
        int questionPartNumber,
        int responseNumber,
        bool questionAllowsMultipleResponses)
    {
        const string selectionOptionPrefix = "selection-option-";

        var selectedOptionItems = BuildSelectedOptionItems().ToList();

        return new DataShareRequestQuestionAnswerPartResponseSelectionOption
        {
            OrderWithinAnswerPart = responseNumber,
            SelectedOptionItems = selectedOptionItems
        };

        IEnumerable<DataShareRequestQuestionAnswerPartResponseSelectionOptionItem> BuildSelectedOptionItems()
        {
            var selectedOptionIds = ReadOptionalFormValues(form, $"questionPart{questionPartNumber}Response{responseNumber}OptionsResponse")
                .Where(x => x.StartsWith(selectionOptionPrefix))
                .Select(x => x.Substring(selectionOptionPrefix.Length))
                .Select(Guid.Parse);

            foreach (var selectedOptionId in selectedOptionIds)
            {
                var supplementaryAnswerPart = BuildSupplementaryAnswerPart(selectedOptionId);

                yield return new DataShareRequestQuestionAnswerPartResponseSelectionOptionItem
                {
                    OptionSelectionItemId = selectedOptionId,
                    SupplementaryQuestionAnswerPart = supplementaryAnswerPart
                };
            }

            DataShareRequestQuestionAnswerPart? BuildSupplementaryAnswerPart(Guid selectedOptionId)
            {
                var supplementaryText = ReadOptionalFormValue(form, $"questionPart{questionPartNumber}Response{responseNumber}Option{selectedOptionId}Supplementary");
                if (supplementaryText == null) return null;

                var supplementaryQuestionPartId = Guid.Parse(ReadFormValue(form, $"questionPart{questionPartNumber}Response{responseNumber}Option{selectedOptionId}SupplementaryQuestionPartId"));

                return new DataShareRequestQuestionAnswerPart
                {
                    QuestionPartId = supplementaryQuestionPartId,
                    AnswerPartResponses =
                    [
                        new DataShareRequestQuestionAnswerPartResponseFreeForm
                        {
                            OrderWithinAnswerPart = 1,
                            EnteredValue = supplementaryText,
                            ValueEntryDeclined = false,
                            MultipleResponsesAreAllowed = questionAllowsMultipleResponses
                        }
                    ]
                };
            }
        }
    }

    #region Form Value Reading
    private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("FormReader");
    private static string? ReadOptionalFormValue(
        IFormCollection formCollection,
        string key)
    {
        var values = formCollection[key];

        if (values.Count == 0) return null;

        if (values.Count > 1)
        {
            _logger.LogWarning("Optional value '{Key}' is not unique within form data. Using first occurrence.", key);
        }

        return values.ToString();
    }

    private static string ReadFormValue(
        IFormCollection formCollection,
        string key)
    {
        var values = formCollection[key];

        if (values.Count == 0)
        {
            _logger.LogError("Unable to locate required value '{Key}' in form data.", key);
            return string.Empty;
        }

        if (values.Count > 1)
        {
            _logger.LogWarning("Required value '{Key}' is not unique within form data. Using first occurrence.", key);
        }

        var value = values.ToString();

        // Return LF rather than CRLF
        return value.ReplaceLineEndings("\r");
    }

    private static IEnumerable<string> ReadFormValues(
        IFormCollection formCollection,
        string key)
    {
        var values = formCollection[key];

        if (values.Count == 0)
        {
            _logger.LogWarning("Unable to locate values '{key}' in form data.", key);
        }

        return values;
    }

    private static IEnumerable<string> ReadOptionalFormValues(
        IFormCollection formCollection,
        string key)
    {
        return formCollection[key];
    }
    #endregion
    #endregion
}