export interface CleanCodeRating {
  variableNaming: number;
  functionSizes: number;
  noNeedsComments: number;
  methodCohesion: number;
  deadCode: number;
  note: number;
  quality: string;
  justifications?: Record<string, string>;
  justifies?: Record<string, string>;
}

export enum RatingQuality {
  Excellent = 'Excellent',
  VeryGood = 'VeryGood',
  Good = 'Good',
  Acceptable = 'Acceptable',
  NeedsImprovement = 'NeedsImprovement',
  Problematic = 'Problematic'
}
