using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// Guides the Home introduction and records its completion.
    /// </summary>
    public class HomeTutorialFlow
    {
        private enum Step
        {
            Inactive,
            Lives,
            Diamonds,
            Map,
            OpeningChapters,
            Chapters,
            ChapterClose,
            ClosingChapters,
            Shop,
            OpeningShop,
            ShopExplanation,
            Saving
        }

        private readonly HomeTutorialConfig homeTutorialConfig;
        private readonly PlayFabTutorialProgressService playFabTutorialProgressService = new PlayFabTutorialProgressService();
        private CancellationTokenSource tutorialCancellationTokenSource;
        private Step currentStep;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private bool isHomeTutorialPreview;
#endif

        public bool IsHomeTutorialActive => currentStep != Step.Inactive;

        public HomeTutorialFlow(HomeTutorialConfig homeTutorialConfig)
        {
            this.homeTutorialConfig = homeTutorialConfig;
        }

        public void StartHomeTutorial()
        {
            if (IsHomeTutorialActive) throw new InvalidOperationException("The Home introduction is already active.");
            if (PlayerAccountContext.Instance.CurrentAccount.PlayerTutorialProgressData.completedTutorialIds.Contains(TutorialProgressContract.HomeIntroductionTutorialId)) return;

            BeginHomeTutorial();
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public void StartHomeTutorialPreview()
        {
            isHomeTutorialPreview = true;
            BeginHomeTutorial();
        }
#endif

        public void StopHomeTutorial()
        {
            if (!IsHomeTutorialActive) return;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            isHomeTutorialPreview = false;
#endif
            currentStep = Step.Inactive;
            UIManager.Instance.TutorialContinueRequested -= HandleTutorialContinueRequested;
            UIManager.Instance.TutorialSkipRequested -= HandleTutorialSkipRequested;
            tutorialCancellationTokenSource.Cancel();
            tutorialCancellationTokenSource.Dispose();
            tutorialCancellationTokenSource = null;
            UIManager.Instance.HideTutorial();
            UIManager.Instance.ClearHomeTutorialInteraction();
        }

        /// <summary>
        /// Accepts only the tab requested by the current step.
        /// </summary>
        public bool TryBeginHomeTutorialTabNavigation(HomeMiddleTab tab)
        {
            if (!Enum.IsDefined(typeof(HomeMiddleTab), tab)) throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown Home tab.");
            if (!IsHomeTutorialActive) return true;
            if (currentStep == Step.Map && tab == HomeMiddleTab.Map)
            {
                currentStep = Step.OpeningChapters;
                UIManager.Instance.BlockHomeTutorialInteraction();
                return true;
            }
            if (currentStep == Step.Shop && tab == HomeMiddleTab.Shop)
            {
                currentStep = Step.OpeningShop;
                UIManager.Instance.BlockHomeTutorialInteraction();
                return true;
            }
            return false;
        }

        public bool TryBeginHomeTutorialChapterClose()
        {
            if (!IsHomeTutorialActive) return true;
            if (currentStep != Step.ChapterClose) return false;
            currentStep = Step.ClosingChapters;
            UIManager.Instance.BlockHomeTutorialInteraction();
            return true;
        }

        public void HandleHomeChapterChooserShown()
        {
            if (!IsHomeTutorialActive) return;
            if (currentStep != Step.OpeningChapters) throw new InvalidOperationException($"The chapter chooser opened during tutorial step {currentStep}.");
            DisplayTutorialStep(Step.Chapters);
        }

        public void HandleHomeChapterChooserHidden()
        {
            if (!IsHomeTutorialActive) return;
            if (currentStep != Step.ClosingChapters) throw new InvalidOperationException($"The chapter chooser closed during tutorial step {currentStep}.");
            DisplayTutorialStep(Step.Shop);
        }

        public void HandleHomeShopNavigationFinished(bool shopDisplayed)
        {
            if (!IsHomeTutorialActive) return;
            if (currentStep != Step.OpeningShop) throw new InvalidOperationException($"The shop navigation finished during tutorial step {currentStep}.");
            DisplayTutorialStep(shopDisplayed ? Step.ShopExplanation : Step.Shop);
        }

        private void BeginHomeTutorial()
        {
            tutorialCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken, UIManager.Instance.destroyCancellationToken);
            UIManager.Instance.TutorialContinueRequested += HandleTutorialContinueRequested;
            UIManager.Instance.TutorialSkipRequested += HandleTutorialSkipRequested;
            DisplayTutorialStep(Step.Lives);
        }

        private void HandleTutorialContinueRequested()
        {
            switch (currentStep)
            {
                case Step.Lives:
                    DisplayTutorialStep(Step.Diamonds);
                    break;
                case Step.Diamonds:
                    DisplayTutorialStep(Step.Map);
                    break;
                case Step.Chapters:
                    DisplayTutorialStep(Step.ChapterClose);
                    break;
                case Step.ShopExplanation:
                    CompleteHomeTutorial();
                    break;
                default:
                    throw new InvalidOperationException($"The Home introduction cannot continue from step {currentStep}.");
            }
        }

        private void HandleTutorialSkipRequested()
        {
            CompleteHomeTutorial();
        }

        private void CompleteHomeTutorial()
        {
            if (!IsHomeTutorialActive || currentStep == Step.Saving) return;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (isHomeTutorialPreview)
            {
                StopHomeTutorial();
                return;
            }
#endif
            currentStep = Step.Saving;
            UIManager.Instance.BlockHomeTutorialInteraction();
            CancellationToken tutorialCancellationToken = tutorialCancellationTokenSource.Token;
            ApplicationOperationRunner.Instance.Run(() => SaveHomeTutorialCompletion(tutorialCancellationToken));
        }

        private void DisplayTutorialStep(Step step)
        {
            currentStep = step;
            switch (step)
            {
                case Step.Lives:
                    UIManager.Instance.ShowHomeTutorialStep(HomeTutorialTarget.Lives, homeTutorialConfig.LivesMessage, homeTutorialConfig.ContinueLabel, false);
                    break;
                case Step.Diamonds:
                    UIManager.Instance.ShowHomeTutorialStep(HomeTutorialTarget.Diamonds, homeTutorialConfig.DiamondsMessage, homeTutorialConfig.ContinueLabel, false);
                    break;
                case Step.Map:
                    UIManager.Instance.ShowHomeTutorialStep(HomeTutorialTarget.Map, homeTutorialConfig.MapMessage, null, true);
                    break;
                case Step.Chapters:
                    UIManager.Instance.ShowHomeTutorialStep(HomeTutorialTarget.Chapters, homeTutorialConfig.ChaptersMessage, homeTutorialConfig.ContinueLabel, false);
                    break;
                case Step.ChapterClose:
                    UIManager.Instance.ShowHomeTutorialStep(HomeTutorialTarget.ChapterClose, homeTutorialConfig.CloseChaptersMessage, null, true);
                    break;
                case Step.Shop:
                    UIManager.Instance.ShowHomeTutorialStep(HomeTutorialTarget.Shop, homeTutorialConfig.ShopMessage, null, true);
                    break;
                case Step.ShopExplanation:
                    UIManager.Instance.ShowHomeTutorialStep(HomeTutorialTarget.ShopContent, homeTutorialConfig.ShopExplanationMessage, homeTutorialConfig.DoneLabel, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, "This tutorial step has no instruction.");
            }
        }

        private async UniTask SaveHomeTutorialCompletion(CancellationToken tutorialCancellationToken)
        {
            PlayerAccount playerAccount = PlayerAccountContext.Instance.CurrentAccount;
            try
            {
                while (true)
                {
                    tutorialCancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        CompleteTutorialResponse completeTutorialResponse = await ApplicationPresentationService.Instance.RunWithLoading(() => playFabTutorialProgressService.CompleteTutorial(playerAccount.AuthSession, TutorialProgressContract.HomeIntroductionTutorialId)).AttachExternalCancellation(tutorialCancellationToken);
                        tutorialCancellationToken.ThrowIfCancellationRequested();
                        playerAccount.ApplyConfirmedTutorialProgress(completeTutorialResponse.playerTutorialProgressData);
                        StopHomeTutorial();
                        return;
                    }
                    catch (PlayFabRequestException exception) when (exception.IsRetryable)
                    {
                        Debug.LogWarning(exception);
                        if (await DialogManager.Instance.RunRetryOrCancelDialog(homeTutorialConfig.SaveFailureTitle, homeTutorialConfig.SaveRetryMessage, tutorialCancellationToken)) continue;
                        StopHomeTutorial();
                        return;
                    }
                    catch (PlayFabRequestException exception)
                    {
                        Debug.LogWarning(exception);
                        await DialogManager.Instance.RunOkDialog(homeTutorialConfig.SaveFailureTitle, homeTutorialConfig.SaveFailureMessage, tutorialCancellationToken);
                        StopHomeTutorial();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (tutorialCancellationToken.IsCancellationRequested)
            {
            }
        }
    }
}
