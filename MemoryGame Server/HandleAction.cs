namespace MemoryGameServerWPF
{
    public class HandleAction
    {

        public ActionMessage DetermineNextAction(GameState _gameState, int cardIndex)
        {
            var actionMessage = new ActionMessage
            {
                ActionType = ActionType.FirstCard,
                ClickedCardIndex = cardIndex
            };
            if (_gameState.PreviousCard != null)
            {
                actionMessage.ActionType = ActionType.SecondCard;
                return actionMessage;
            }
            else
            {
                return actionMessage;
            }
        }
        public void FlipCard(ActionMessage actionMessage, GameState gameState)
        {
            int cardIndex = actionMessage.ClickedCardIndex;
            SetCardEnabledState(gameState.Cards[cardIndex], false);
        }
        public void FlipBackCards(ActionMessage actionMessage, GameState gameState)
        {
            SetCardEnabledState(gameState.PreviousCard, true);
            SetCardEnabledState(gameState.Cards[actionMessage.ClickedCardIndex], true);
            Logger.Instance.Log("Beide Karten werden wieder verdeckt.");
        }
        public ActionMessage CheckForMatch(ActionMessage actionMessage, GameState gameState, GameHelper _gameHelper)
        {
            var clickedCard = gameState.Cards[actionMessage.ClickedCardIndex];

            if (gameState.PreviousCard.ID != clickedCard.ID)
            {
                FlipBackCards(actionMessage, gameState);
                actionMessage.ActionType = ActionType.NoMatch;
                _gameHelper.SwitchCurrentPlayer(gameState);
            }
            else
            {
                _gameHelper.AwardPoint(gameState);
                _gameHelper.FoundPairs.Add(gameState.PreviousCard.ID);
                actionMessage.ActionType = ActionType.MatchFound;
            }
            return actionMessage;

        }
        private void SetCardEnabledState(Card card, bool isEnabled)
        {
            if (card != null)
            {
                card.IsEnabled = isEnabled;
            }
        }
    }

}
