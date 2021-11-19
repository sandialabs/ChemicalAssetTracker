<template>
    <div class="hello">Hello {{who}}</div>
</template>

<script>

    console.log("This is test.vue");
    module.exports = {
        data: function() {
            return {
                who: 'world'
            }
    }
    console.log("TestView: ", TestView);
}
</script>

<style>
    .hello {
        background-color: #ffe;
    }
</style>