import { CleanCodeRating } from './CleanCodeRating';
import { CommitFile } from './CommitFile';
import { Suggestion } from './Suggestion';

export interface CommitAnalysis {
  id: string;
  commitId: string;
  author: string;
  email: string;
  commitDate: string;
  analysisDate: string;
  language: string;
  addedLines: number;
  removedLines: number;
  files: CommitFile[];
  suggestions: Suggestion[];
  rating?: CleanCodeRating;
  overallNote: number;
}
