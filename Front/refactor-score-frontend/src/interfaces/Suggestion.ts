export interface Suggestion {
  title: string;
  description: string;
  priority: string;
  type: string;
  difficult: string;
  fileReference: string;
  lastUpdate: string;
  studyResources: string[];
}

export enum SuggestionPriority {
  High = 'High',
  Medium = 'Medium',
  Low = 'Low'
}

export enum SuggestionDifficulty {
  Easy = 'Easy',
  Medium = 'Medium',
  Hard = 'Hard'
}
