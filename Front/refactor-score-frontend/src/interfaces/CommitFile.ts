import { CleanCodeRating } from './CleanCodeRating';
import { Suggestion } from './Suggestion';

export interface CommitFile {
  id?: string;
  fileId?: string;
  path: string;
  language: string;
  addedLines: number;
  removedLines: number;
  content?: string;
  hasAnalysis?: boolean;
  rating?: CleanCodeRating;
  suggestions?: Suggestion[];
  perFileSum?: number;
}
