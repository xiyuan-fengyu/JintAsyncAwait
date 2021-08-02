declare const type: (classFullName: string) => any;
declare const require: (classFullName: string) => any;
declare const log: (...args: any[]) => void;
declare const setTimeout: (callback: () => void, ms: number) => Promise<void>;
declare const sleep: (ms: number) => Promise<void>;
