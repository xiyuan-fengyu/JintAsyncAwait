
(() => {

    class Test {
        
        test() {
            log(new Date());
        }
        
        async testAsync() {
            await new Promise<any>(resolve => {
                setTimeout(resolve, 1000);
            });
            for (let i = 0; i < 10; i++) {
                await sleep(1000);
                log(`${i}: ${new Date().getTime()}`)
            }
        }
        
    }
    
    return new Test();
})();
