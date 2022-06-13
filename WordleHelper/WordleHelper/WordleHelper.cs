using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WordleHelper
{
    public class MyWordleHelper
    {
        public enum RESULT
        {
            CORRECT,
            CORRECT_BUT_WRONG_POS,
            WRONG,
            EMPTY
        }

        private MainWindow ui;

        private const string RAW_WORDS_FILE_DIR = "../../words_alpha.txt";
        private const string OP_WORDS_FILE_DIR = "all5LenWords.txt";

        private HashSet<char> setUpperCase = new HashSet<char>();

        private string[] fivies;
        private List<string> filteredFivies;


        private bool initialized = false;
        private int guessIteration = 0;
        private List<char> truth = new List<char>();
        private HashSet<char> greens = new HashSet<char>();
        private HashSet<char> yellows = new HashSet<char>();

        private List<Dictionary<char, double>> listOfMapLetterPossibilities = new List<Dictionary<char, double>>();
        private Dictionary<char, double> mapOfLetterInWordsPossibilities = new Dictionary<char, double>();

        #region INITIALIZE
        public void init(MainWindow ui)
        {
            this.ui = ui;

            setUpperCase.Add('A');
            setUpperCase.Add('B');
            setUpperCase.Add('C');
            setUpperCase.Add('D');
            setUpperCase.Add('E');
            setUpperCase.Add('F');
            setUpperCase.Add('G');
            setUpperCase.Add('H');
            setUpperCase.Add('I');
            setUpperCase.Add('J');
            setUpperCase.Add('K');
            setUpperCase.Add('L');
            setUpperCase.Add('M');
            setUpperCase.Add('N');
            setUpperCase.Add('O');
            setUpperCase.Add('P');
            setUpperCase.Add('Q');
            setUpperCase.Add('R');
            setUpperCase.Add('S');
            setUpperCase.Add('T');
            setUpperCase.Add('U');
            setUpperCase.Add('V');
            setUpperCase.Add('W');
            setUpperCase.Add('X');
            setUpperCase.Add('Y');
            setUpperCase.Add('Z');

            for (int i = 0; i < 5; i++)
            {
                Dictionary<char, double> poss = new Dictionary<char, double>();
                for (int j = 0; j < setUpperCase.Count; j++)
                {
                    poss.Add(setUpperCase.ElementAt(j), 0);
                }
                listOfMapLetterPossibilities.Add(poss);
            }

        }
        #endregion

        #region UTILITIES
        // UTILITIES
        private string toUppercase(string word)
        {
            string lowercase = "";
            foreach (char c in word)
            {
                if (char.IsLower(c))
                {
                    lowercase += char.ToUpper(c);
                }
                else
                {
                    lowercase += c;
                }
            }
            return lowercase;
        }

        public bool isValidChar(char c)
        {
            return setUpperCase.Contains(c);
        }

        public char getValidChar(char c)
        {
            if (setUpperCase.Contains(c))
            {
                return c;
            }
            else if (char.IsLower(c))
            {
                return char.ToUpper(c);
            }
            return '\0';
        }

        #endregion

        #region EXTRAS
        public void extract5LenWords()
        {
            List<string> fivies = new List<string>();

            // open words file and collect all words
            string[] allLines = File.ReadAllLines(RAW_WORDS_FILE_DIR);
            string tLine = "";

            foreach (string line in allLines)
            {
                tLine = line.Trim();
                // filter 5 len words
                //Regex.IsMatch(tLine, "^[a-zA-Z0-9]*$")
                if (tLine.Length == 5 && !tLine.Contains(" "))
                {
                    fivies.Add(toUppercase(tLine));
                }
            }

            // export to file
            using (StreamWriter writer = new StreamWriter(OP_WORDS_FILE_DIR, false))
            {
                foreach (string fivy in fivies)
                {
                    writer.WriteLine(fivy);
                }
            }
        }
        #endregion

        private void restartFilteredWords()
        {
            filteredFivies = new List<string>(fivies);

            // refresh info
            refreshForFilter();
        }

        public void initForSuggestion()
        {
            if (!initialized)
            {
                // read file
                fivies = File.ReadAllLines(OP_WORDS_FILE_DIR);
                restartFilteredWords();
                guessIteration = 0;
                ui.printGuessNum(guessIteration + 1);
                ui.createKeyboard();
                ui.createInputPanels();

                truth.Add('*');
                truth.Add('*');
                truth.Add('*');
                truth.Add('*');
                truth.Add('*');

                initialized = true;
                ui.setInitRestartButton(false);
                ui.setSubmitButtonEnable(true);
            }
            else
            {
                restartFilteredWords();
                guessIteration = 0;
                ui.printGuessNum(guessIteration + 1);
                ui.clearKeyboardPanel();
                ui.clearSelectionPanel();
                clearGuessLists();
                ui.clearPreviousGuesses();
            }
        }

        public void submit()
        {
            if (initialized)
            {
                // ask selection letters
                List<Tuple<char, RESULT>> guess = new List<Tuple<char, RESULT>>();
                for (int i = 0; i < ui.getSelectionLetters().Count; i++)
                {
                    if (ui.getSelectionLetters()[i].isCompleted())
                    {
                        guess.Add(ui.getSelectionLetters()[i].getInfo());
                    }
                    else
                    {
                        // missing info
                        ui.showMessage("Missing Information. Check Guess Panel!");
                        return;
                    }
                }

                // check word
                string wordOfGuess = getWordOfGuess(guess);
                if (filteredFivies.Contains(wordOfGuess))
                {
                    // filter more
                    filterWordsFor(guess);

                    ui.setPreviousGuess(guessIteration, guess);
                    ui.refreshKeyboardLetters(guess);
                    stepOnGuess();
                    ui.clearSelectionPanel();
                }
                else
                {
                    ui.showMessage("No Such Word Exist in Dictionary!");
                }
            }
        }

        #region FILTERING
        private void filterWordsFor(List<Tuple<char, RESULT>> guess)
        {
            // apply greens - green overrides all
            for (int i = 0; i < guess.Count; i++)
            {
                if (guess[i].Item2 == RESULT.CORRECT)
                {
                    applyGreenLetter(i, guess[i].Item1);
                }
            }
            // apply yellows - yellow overrides gray = no yellow and gray allowed for a letter
            for (int i = 0; i < guess.Count; i++)
            {
                if (guess[i].Item2 == RESULT.CORRECT_BUT_WRONG_POS)
                {
                    applyYellowLetter(i, guess[i].Item1);
                }
            }
            // apply grays
            for (int i = 0; i < guess.Count; i++)
            {
                if (guess[i].Item2 == RESULT.WRONG)
                {
                    applyGrayLetter(i, guess[i].Item1);
                }
            }

            // refresh remaining words info
            refreshForFilter();
        }

        private void applyGreenLetter(int index, char c)
        {
            truth[index] = c;
            greens.Add(c);
            // store indexes for safe delete in second iteration
            List<int> toDeleteIndexes = new List<int>();

            // apply only exact matches
            for (int i = 0; i < filteredFivies.Count; i++)
            {
                if (filteredFivies[i][index] != c)
                {
                    toDeleteIndexes.Add(i);
                }
            }

            // delete non-matchers
            for (int i = toDeleteIndexes.Count - 1; i >= 0; i--)
            {
                filteredFivies.RemoveAt(toDeleteIndexes[i]);
            }
        }

        private void applyYellowLetter(int index, char c)
        {
            yellows.Add(c);
            List<int> toDeleteIndexes = new List<int>();

            if (greens.Contains(c))
            {
                // at least one more this char
                for (int i = 0; i < filteredFivies.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < truth.Count; j++)
                    {
                        if (truth[j] == '*')
                        {
                            if (filteredFivies[i][j] == c)
                            {
                                found = true;
                            }
                        }
                    }
                    if (!found)
                    {
                        toDeleteIndexes.Add(i);
                    }

                    // but not this position since its yellow
                    if (filteredFivies[i][index] == c)
                    {
                        toDeleteIndexes.Add(i);
                    }
                }
            }
            else
            {
                // at least one
                for (int i = 0; i < filteredFivies.Count; i++)
                {
                    if (!filteredFivies[i].Contains(c))
                    {
                        toDeleteIndexes.Add(i);
                    }

                    // but not this position since its yellow
                    if (filteredFivies[i][index] == c)
                    {
                        toDeleteIndexes.Add(i);
                    }
                }
            }

            // delete non-matchers
            for (int i = toDeleteIndexes.Count - 1; i >= 0; i--)
            {
                filteredFivies.RemoveAt(toDeleteIndexes[i]);
            }
        }

        private void applyGrayLetter(int index, char c)
        {
            List<int> toDeleteIndexes = new List<int>();

            if (greens.Contains(c))
            {
                // no more this char
                for (int i = 0; i < filteredFivies.Count; i++)
                {
                    for (int j = 0; j < truth.Count; j++)
                    {
                        if (truth[j] == '*')
                        {
                            if (filteredFivies[i][j] == c)
                            {
                                toDeleteIndexes.Add(i);
                            }
                        }
                    }
                }
            }
            else
            {
                // not this char
                for (int i = 0; i < filteredFivies.Count; i++)
                {
                    if (filteredFivies[i].Contains(c))
                    {
                        toDeleteIndexes.Add(i);
                    }
                }
            }

            // delete non-matchers
            for (int i = toDeleteIndexes.Count - 1; i >= 0; i--)
            {
                filteredFivies.RemoveAt(toDeleteIndexes[i]);
            }
        }

        private void refreshForFilter()
        {
            ui.refreshRemainingWordsLB(filteredFivies);
            ui.refreshLetterPossibilities();
            ui.refreshWordSuggestions(filteredFivies);
        }
        #endregion

        #region SUGGESTING
        public List<Tuple<char, double>> analizeLetterPossibility(int index)
        {
            for (int i = 0; i < setUpperCase.Count; i++)
            {
                listOfMapLetterPossibilities[index][setUpperCase.ElementAt(i)] = 0;
            }

            List<Tuple<char, double>> possibilities = new List<Tuple<char, double>>();
            if (index >= 0 && index < 5)
            {
                Dictionary<char, int> mapLetterCounts = new Dictionary<char, int>();
                foreach (string word in filteredFivies)
                {
                    if (mapLetterCounts.ContainsKey(word.ElementAt(index)))
                    {
                        mapLetterCounts[word.ElementAt(index)]++;
                    }
                    else
                    {
                        mapLetterCounts[word.ElementAt(index)] = 1;
                    }
                }

                char mostP = ' ';
                int mostPCount = 0;
                char secondP = ' ';
                int secondPCount = 0;
                char thirdP = ' ';
                int thirdPCount = 0;
                int totalP = 0;
                Dictionary<char, int>.KeyCollection mapLetters = mapLetterCounts.Keys;
                foreach (char letter in mapLetters)
                {
                    if (mapLetterCounts[letter] > mostPCount)
                    {
                        thirdP = secondP;
                        thirdPCount = secondPCount;
                        secondP = mostP;
                        secondPCount = mostPCount;
                        mostP = letter;
                        mostPCount = mapLetterCounts[letter];
                    }
                    else if (mapLetterCounts[letter] > secondPCount)
                    {
                        thirdP = secondP;
                        thirdPCount = secondPCount;
                        secondP = letter;
                        secondPCount = mapLetterCounts[letter];
                    }
                    else if (mapLetterCounts[letter] > thirdPCount)
                    {
                        thirdP = letter;
                        thirdPCount = mapLetterCounts[letter];
                    }
                    totalP += mapLetterCounts[letter];
                }
                possibilities.Add(new Tuple<char, double>(mostP, (double)mostPCount / totalP));
                possibilities.Add(new Tuple<char, double>(secondP, (double)secondPCount / totalP));
                possibilities.Add(new Tuple<char, double>(thirdP, (double)thirdPCount / totalP));

                foreach (char letter in mapLetters)
                {
                    listOfMapLetterPossibilities[index][letter] = (double)mapLetterCounts[letter] / totalP;
                }
            }

            // count letter in word occurence possibilities
            Dictionary<char, int> mapLetterinWordCounts = new Dictionary<char, int>();
            foreach (string word in filteredFivies)
            {
                for (int i = 0; i < setUpperCase.Count; i++)
                {
                    if (word.Contains(setUpperCase.ElementAt(i)))
                    {
                        if (mapLetterinWordCounts.ContainsKey(setUpperCase.ElementAt(i)))
                        {
                            mapLetterinWordCounts[setUpperCase.ElementAt(i)]++;
                        }
                        else
                        {
                            mapLetterinWordCounts[setUpperCase.ElementAt(i)] = 1;
                        }
                    }
                }
            }
            for (int i = 0; i < setUpperCase.Count; i++)
            {
                if (mapLetterinWordCounts.ContainsKey(setUpperCase.ElementAt(i)))
                {
                    mapOfLetterInWordsPossibilities[setUpperCase.ElementAt(i)] = (double)mapLetterinWordCounts[setUpperCase.ElementAt(i)] / filteredFivies.Count;
                }
                else
                {
                    mapOfLetterInWordsPossibilities[setUpperCase.ElementAt(i)] = 0;
                }
            }

            return possibilities;
        }


        #endregion

        #region UI_CALLS
        public double getGreenPossibility(int pos, char letter)
        {
            return listOfMapLetterPossibilities[pos][letter];
        }

        public double getYellowPossibility(char letter)
        {
            return mapOfLetterInWordsPossibilities[letter];
        }

        public List<string> getFilteredWords()
        {
            return filteredFivies;
        }
        #endregion

        private void stepOnGuess()
        {
            guessIteration++;
            ui.stepOnGuess(guessIteration);
        }

        private void clearGuessLists()
        {
            for (int i = 0; i < truth.Count; i++)
            {
                truth[i] = '*';
            }

            greens.Clear();
            yellows.Clear();
        }

        private string getWordOfGuess(List<Tuple<char, RESULT>> guess)
        {
            string word = "";
            for (int i = 0; i < guess.Count; i++)
            {
                word += guess[i].Item1;
            }
            return word;
        }

    }
}
