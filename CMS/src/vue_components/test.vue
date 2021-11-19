<template>
    <div class="hello">Hello {{who}}</div>
</template>

<script>

    console.log("This is test.vue");
    const mymodule = {
        props: [ 'who' ],
        data: function() {
            return {
                xwho: 'world'
            }
        }
    }
    module.exports = mymodule;
    if (window.VueComponents) window.VueComponents['Test'] = mymodule;
    else window.VueComponents = { Test: mymodule };
    
</script>

<style>
    .hello {
        background-color: #ffe;
    }
</style>